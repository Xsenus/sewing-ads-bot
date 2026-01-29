using SewingAdsBot.Api.Telegram;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SewingAdsBot.Api.Data;
using SewingAdsBot.Api.Domain.Entities;
using SewingAdsBot.Api.Domain.Enums;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace SewingAdsBot.Api.Services;

/// <summary>
/// Логика модерации: создание заявок, уведомление админов, одобрение/отклонение.
/// </summary>
public sealed class ModerationService
{
    private readonly AppDbContext _db;
    private readonly TelegramBotClientFactory _botFactory;
    private readonly TelegramPublisher _publisher;
    private readonly PostFormatter _formatter;
    private readonly ILogger<ModerationService> _logger;

    /// <summary>
    /// Конструктор.
/// </summary>
    public ModerationService(
        AppDbContext db,
        TelegramBotClientFactory botFactory,
        TelegramPublisher publisher,
        PostFormatter formatter,
        ILogger<ModerationService> logger)
    {
        _db = db;
        _botFactory = botFactory;
        _publisher = publisher;
        _formatter = formatter;
        _logger = logger;
    }

    /// <summary>
    /// Создать заявку на модерацию и отправить её администраторам.
/// </summary>
    public async Task CreateRequestAsync(Guid adId, Guid channelId)
    {
        var exists = await _db.ModerationRequests.AnyAsync(x =>
            x.AdId == adId &&
            x.ChannelId == channelId &&
            x.Status == ModerationStatus.Pending);

        if (exists)
            return;

        var req = new ModerationRequest
        {
            AdId = adId,
            ChannelId = channelId,
            Status = ModerationStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.ModerationRequests.Add(req);
        await _db.SaveChangesAsync();

        await NotifyAdminsAsync(req.Id);
    }

    /// <summary>
    /// Одобрить заявку на модерацию.
/// </summary>
    public async Task<(bool ok, string message)> ApproveAsync(Guid requestId, long? adminTelegramUserId)
    {
        var req = await _db.ModerationRequests.FirstOrDefaultAsync(x => x.Id == requestId);
        if (req == null)
            return (false, "Заявка не найдена.");

        if (req.Status != ModerationStatus.Pending)
            return (false, "Заявка уже обработана.");

        var ad = await _db.Ads.FirstOrDefaultAsync(x => x.Id == req.AdId);
        if (ad == null)
            return (false, "Объявление не найдено.");

        var category = await _db.Categories.FirstOrDefaultAsync(x => x.Id == ad.CategoryId);
        if (category == null)
            return (false, "Категория не найдена.");

        var channel = await _db.Channels.FirstOrDefaultAsync(x => x.Id == req.ChannelId);
        if (channel == null)
            return (false, "Канал не найден.");

        var text = _formatter.BuildPostText(ad, category, channel);

        var (_, link) = await _publisher.PublishAsync(ad, category, channel, text, isBump: false);

        req.Status = ModerationStatus.Approved;
        req.ReviewedByTelegramUserId = adminTelegramUserId;
        req.ReviewedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        await RecalculateAdStatusAsync(ad.Id);
        await NotifyUserAfterModerationAsync(ad.Id, approved: true, link);

        return (true, "Одобрено и опубликовано.");
    }

    /// <summary>
    /// Отклонить заявку на модерацию.
/// </summary>
    public async Task<(bool ok, string message)> RejectAsync(Guid requestId, long? adminTelegramUserId, string? reason = null)
    {
        var req = await _db.ModerationRequests.FirstOrDefaultAsync(x => x.Id == requestId);
        if (req == null)
            return (false, "Заявка не найдена.");

        if (req.Status != ModerationStatus.Pending)
            return (false, "Заявка уже обработана.");

        req.Status = ModerationStatus.Rejected;
        req.ReviewedByTelegramUserId = adminTelegramUserId;
        req.ReviewedAtUtc = DateTime.UtcNow;
        req.RejectReason = string.IsNullOrWhiteSpace(reason) ? "Отклонено модератором" : reason;

        await _db.SaveChangesAsync();

        await RecalculateAdStatusAsync(req.AdId);
        await NotifyUserAfterModerationAsync(req.AdId, approved: false, link: null, reason: req.RejectReason);

        return (true, "Отклонено.");
    }

    /// <summary>
    /// Отправить заявку всем активным Telegram-админам.
/// </summary>
    private async Task NotifyAdminsAsync(Guid requestId)
    {
        var req = await _db.ModerationRequests.FirstOrDefaultAsync(x => x.Id == requestId);
        if (req == null)
            return;

        var ad = await _db.Ads.FirstOrDefaultAsync(x => x.Id == req.AdId);
        if (ad == null)
            return;

        var category = await _db.Categories.FirstOrDefaultAsync(x => x.Id == ad.CategoryId);
        var channel = await _db.Channels.FirstOrDefaultAsync(x => x.Id == req.ChannelId);
        if (category == null || channel == null)
            return;

        var admins = await _db.TelegramAdmins.Where(x => x.IsActive).ToListAsync();
        if (admins.Count == 0)
        {
            _logger.LogWarning("Нет Telegram админов для модерации. Заявка {RequestId} зависла.", requestId);
            return;
        }

        var bot = await _botFactory.GetClientAsync();
        if (bot == null)
            throw new InvalidOperationException("Telegram.BotToken не настроен. Модерация невозможна.");

        var preview = _formatter.BuildModerationPreview(ad, category, channel);

        var kb = new InlineKeyboardMarkup(new[]
        {
            new []
            {
                InlineKeyboardButton.WithCallbackData("✅ Одобрить", $"mod:approve:{requestId}"),
                InlineKeyboardButton.WithCallbackData("❌ Отклонить", $"mod:reject:{requestId}")
            }
        });

        foreach (var admin in admins)
        {
            try
            {
                await bot.SendTextMessageAsync(
                    chatId: admin.TelegramUserId,
                    text: preview,
                    parseMode: ParseMode.Html,
                    replyMarkup: kb);

                _logger.LogInformation("Заявка {RequestId} отправлена админу {AdminId}.", requestId, admin.TelegramUserId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Не удалось отправить заявку {RequestId} админу {AdminId}.", requestId, admin.TelegramUserId);
            }
        }
    }

    /// <summary>
    /// Пересчитать статус объявления по итогам публикаций/модераций.
/// </summary>
    private async Task RecalculateAdStatusAsync(Guid adId)
    {
        var ad = await _db.Ads.FirstOrDefaultAsync(x => x.Id == adId);
        if (ad == null) return;

        var hasPublished = await _db.AdPublications.AnyAsync(x => x.AdId == adId);
        var hasPending = await _db.ModerationRequests.AnyAsync(x => x.AdId == adId && x.Status == ModerationStatus.Pending);

        if (hasPublished)
            ad.Status = AdStatus.Published;
        else if (hasPending)
            ad.Status = AdStatus.PendingModeration;
        else
            ad.Status = AdStatus.Rejected;

        ad.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Уведомить пользователя об итогах модерации (и прислать ссылку после публикации).
    /// </summary>
    private async Task NotifyUserAfterModerationAsync(Guid adId, bool approved, string? link, string? reason = null)
    {
        var ad = await _db.Ads.FirstOrDefaultAsync(x => x.Id == adId);
        if (ad == null) return;

        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == ad.UserId);
        if (user == null) return;

        var bot = await _botFactory.GetClientAsync();
        if (bot == null)
            throw new InvalidOperationException("Telegram.BotToken не настроен. Модерация невозможна.");

        if (approved)
        {
            var msg = link == null
                ? "Ваше объявление прошло модерацию и опубликовано."
                : $"Ваше объявление прошло модерацию и опубликовано: {link}";

            await bot.SendTextMessageAsync(user.TelegramUserId, msg);
        }
        else
        {
            var msg = string.IsNullOrWhiteSpace(reason)
                ? "Ваше объявление отклонено модератором."
                : $"Ваше объявление отклонено модератором.\nПричина: {reason}";

            await bot.SendTextMessageAsync(user.TelegramUserId, msg);
        }
    }
}
