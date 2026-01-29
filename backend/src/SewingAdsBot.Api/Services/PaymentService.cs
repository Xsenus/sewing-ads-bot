using SewingAdsBot.Api.Telegram;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SewingAdsBot.Api.Data;
using SewingAdsBot.Api.Domain.Entities;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Payments;

namespace SewingAdsBot.Api.Services;

/// <summary>
/// Интеграция Telegram Payments:
/// - создание инвойсов (платное объявление / платное поднятие)
/// - обработка pre-checkout и successful payment
/// </summary>
public sealed class PaymentService
{
    private readonly AppDbContext _db;
    private readonly TelegramBotClientFactory _botFactory;
    private readonly SettingsService _settings;
    private readonly PublicationService _publicationService;
    private readonly ReferralService _referralService;
    private readonly ILogger<PaymentService> _logger;

    /// <summary>
    /// Конструктор.
/// </summary>
    public PaymentService(
        AppDbContext db,
        TelegramBotClientFactory botFactory,
        SettingsService settings,
        PublicationService publicationService,
        ReferralService referralService,
        ILogger<PaymentService> logger)
    {
        _db = db;
        _botFactory = botFactory;
        _settings = settings;
        _publicationService = publicationService;
        _referralService = referralService;
        _logger = logger;
    }

    /// <summary>
    /// Отправить пользователю инвойс на оплату платного объявления.
/// </summary>
    public async Task SendPaidAdInvoiceAsync(long telegramUserId, Guid adId)
    {
        var priceMinor = await _settings.GetIntAsync("PaidAdPriceMinor", 20000); // 200.00 RUB
        await SendInvoiceAsync(telegramUserId, kind: "PaidAd", adId, priceMinor, "RUB",
            title: "Платное объявление",
            description: "Публикация платного объявления (можно фото/видео и ссылки).");
    }

    /// <summary>
    /// Отправить пользователю инвойс на оплату поднятия объявления.
/// </summary>
    public async Task SendBumpInvoiceAsync(long telegramUserId, Guid adId)
    {
        var priceMinor = await _settings.GetIntAsync("BumpPriceMinor", 5000); // 50.00 RUB
        await SendInvoiceAsync(telegramUserId, kind: "Bump", adId, priceMinor, "RUB",
            title: "Поднятие объявления",
            description: "Повторная публикация вашего объявления в ленте (bump).");
    }

    /// <summary>
    /// Обработка PreCheckoutQuery (Telegram Payments).
    /// </summary>
    public async Task HandlePreCheckoutAsync(PreCheckoutQuery query)
    {
        var bot = await _botFactory.GetClientAsync();
        if (bot == null)
            throw new InvalidOperationException("Telegram.BotToken не настроен. Платежи невозможны.");

        var inv = await _db.PaymentInvoices.FirstOrDefaultAsync(x => x.Payload == query.InvoicePayload);
        if (inv == null || inv.IsPaid)
        {
            await bot.AnswerPreCheckoutQueryAsync(query.Id, false, "Инвойс не найден или уже оплачен.");
            return;
        }

        await bot.AnswerPreCheckoutQueryAsync(query.Id, true);
    }

    /// <summary>
    /// Обработка SuccessfulPayment (Telegram Payments).
    /// </summary>
    public async Task HandleSuccessfulPaymentAsync(Message message)
    {
        if (message.SuccessfulPayment == null)
            return;

        var payload = message.SuccessfulPayment.InvoicePayload;

        var inv = await _db.PaymentInvoices.FirstOrDefaultAsync(x => x.Payload == payload);
        if (inv == null)
        {
            _logger.LogWarning("SuccessfulPayment: инвойс не найден по payload={Payload}", payload);
            return;
        }

        if (inv.IsPaid)
            return;

        inv.IsPaid = true;
        inv.PaidAtUtc = DateTime.UtcNow;
        inv.TelegramChargeId = message.SuccessfulPayment.TelegramPaymentChargeId;

        await _db.SaveChangesAsync();

        // Реферальное вознаграждение (на внутренний баланс)
        await _referralService.ApplyReferralRewardAsync(inv.TelegramUserId, inv.AmountMinor, inv.Currency);
        var bot = await _botFactory.GetClientAsync();
        if (bot == null)
            throw new InvalidOperationException("Telegram.BotToken не настроен. Платежи невозможны.");

        if (inv.Kind == "PaidAd" && inv.AdId.HasValue)
        {
            var pub = await _publicationService.SubmitAsync(inv.TelegramUserId, inv.AdId.Value);

            if (pub.Ok && pub.PublishedLinks.Count > 0)
            {
                await bot.SendTextMessageAsync(inv.TelegramUserId, "Оплата прошла успешно ✅\n" +
                                                               "Ссылки на публикации:\n" +
                                                               string.Join("\n", pub.PublishedLinks));
            }
            else
            {
                await bot.SendTextMessageAsync(inv.TelegramUserId, "Оплата прошла успешно ✅\n" + pub.Message);
            }
        }
        else if (inv.Kind == "Bump" && inv.AdId.HasValue)
        {
            var links = await _publicationService.BumpAsync(inv.TelegramUserId, inv.AdId.Value);

            if (links.Count > 0)
            {
                await bot.SendTextMessageAsync(inv.TelegramUserId, "Поднятие выполнено ✅\n" +
                                                               "Новые ссылки:\n" +
                                                               string.Join("\n", links));
            }
            else
            {
                await bot.SendTextMessageAsync(inv.TelegramUserId, "Поднятие выполнено ✅\n" +
                                                               "Но ссылки не удалось сформировать (каналы без username).");
            }
        }
        else
        {
            await bot.SendTextMessageAsync(inv.TelegramUserId, "Оплата прошла успешно ✅");
        }
    }

    private async Task SendInvoiceAsync(
        long telegramUserId,
        string kind,
        Guid adId,
        int amountMinor,
        string currency,
        string title,
        string description)
    {
        var providerToken = await _botFactory.GetPaymentProviderTokenAsync();
        if (string.IsNullOrWhiteSpace(providerToken))
            throw new InvalidOperationException(
                "Не задан Telegram.PaymentProviderToken. Задайте его в админке (Settings: Telegram.PaymentProviderToken) или в appsettings.json.");

        var bot = await _botFactory.GetClientAsync();
        if (bot == null)
            throw new InvalidOperationException("Telegram.BotToken не настроен. Платежи невозможны.");

        var payload = $"{kind}:{Guid.NewGuid():N}";

        var inv = new PaymentInvoice
        {
            TelegramUserId = telegramUserId,
            Kind = kind,
            AdId = adId,
            AmountMinor = amountMinor,
            Currency = currency,
            Payload = payload,
            IsPaid = false,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.PaymentInvoices.Add(inv);
        await _db.SaveChangesAsync();

        var prices = new List<LabeledPrice>
        {
            new(title, amountMinor)
        };

        await bot.SendInvoiceAsync(
            chatId: telegramUserId,
            title: title,
            description: description,
            payload: payload,
            providerToken: providerToken,
            currency: currency,
            prices: prices);
    }
}
