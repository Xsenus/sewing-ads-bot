using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SewingAdsBot.Api.Data;
using SewingAdsBot.Api.Domain.Entities;
using SewingAdsBot.Api.Domain.Enums;
using AppUser = SewingAdsBot.Api.Domain.Entities.User;
using SewingAdsBot.Api.Options;
using SewingAdsBot.Api.Services;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace SewingAdsBot.Api.Telegram;

/// <summary>
/// Центральный обработчик Telegram updates (сообщения, callback'и, платежи).
/// </summary>
public sealed class BotUpdateHandler
{
    private readonly AppDbContext _db;
    private readonly UserService _users;
    private readonly CategoryService _categories;
    private readonly AdService _ads;
    private readonly PublicationService _publication;
    private readonly PaymentService _payments;
    private readonly PostFormatter _formatter;
    private readonly SearchService _search;
    private readonly ModerationService _moderation;
    private readonly SettingsService _settings;
    private readonly AppOptions _appOptions;
    private readonly ILogger<BotUpdateHandler> _logger;

    /// <summary>
    /// Конструктор.
/// </summary>
    public BotUpdateHandler(
        AppDbContext db,
        UserService users,
        CategoryService categories,
        AdService ads,
        PublicationService publication,
        PaymentService payments,
        PostFormatter formatter,
        SearchService search,
        ModerationService moderation,
        SettingsService settings,
        IOptions<AppOptions> appOptions,
        ILogger<BotUpdateHandler> logger)
    {
        _db = db;
        _users = users;
        _categories = categories;
        _ads = ads;
        _publication = publication;
        _payments = payments;
        _formatter = formatter;
        _search = search;
        _moderation = moderation;
        _settings = settings;
        _appOptions = appOptions.Value;
        _logger = logger;
    }

    /// <summary>
    /// Обработка входящего обновления.
/// </summary>
    public async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken ct)
    {
        switch (update.Type)
        {
            case UpdateType.PreCheckoutQuery:
                if (update.PreCheckoutQuery != null)
                    await _payments.HandlePreCheckoutAsync(update.PreCheckoutQuery);
                return;

            case UpdateType.CallbackQuery:
                if (update.CallbackQuery != null)
                    await HandleCallbackAsync(bot, update.CallbackQuery, ct);
                return;

            case UpdateType.Message:
                if (update.Message != null)
                    await HandleMessageAsync(bot, update.Message, ct, isEdited: false);
                return;

            case UpdateType.EditedMessage:
                if (update.EditedMessage != null)
                    await HandleMessageAsync(bot, update.EditedMessage, ct, isEdited: true);
                return;

            case UpdateType.ChannelPost:
                if (update.ChannelPost != null)
                    await HandleChannelPostAsync(bot, update.ChannelPost, ct, isEdited: false);
                return;

            case UpdateType.EditedChannelPost:
                if (update.EditedChannelPost != null)
                    await HandleChannelPostAsync(bot, update.EditedChannelPost, ct, isEdited: true);
                return;

            default:
                return;
        }
    }

    /// <summary>
    /// Обработка ошибок polling.
/// </summary>
    public Task HandleErrorAsync(ITelegramBotClient bot, Exception exception, CancellationToken ct)
    {
        var msg = exception switch
        {
            ApiRequestException apiEx => $"Telegram API Error:\n[{apiEx.ErrorCode}]\n{apiEx.Message}",
            _ => exception.ToString()
        };

        _logger.LogError(exception, "Telegram polling error: {Message}", msg);
        return Task.CompletedTask;
    }

    private async Task HandleMessageAsync(ITelegramBotClient bot, Message message, CancellationToken ct, bool isEdited)
    {
        await LogIncomingMessageAsync(bot, message, isEdited, ct);

        if (message.Chat.Type != ChatType.Private)
            return;

        var tgUser = message.From;
        if (tgUser == null)
            return;

        // SuccessfulPayment
        if (message.SuccessfulPayment != null)
        {
            await _payments.HandleSuccessfulPaymentAsync(message);
            return;
        }

        var user = await _users.GetOrCreateAsync(tgUser.Id, tgUser.Username, tgUser.FirstName);

        // /start
        if (!string.IsNullOrWhiteSpace(message.Text) && message.Text.StartsWith("/start", StringComparison.OrdinalIgnoreCase))
        {
            await HandleStartAsync(bot, user, message.Text);
            return;
        }

        var state = await _users.GetOrCreateStateAsync(user.TelegramUserId);

        // Глобальная отмена
        if (string.Equals(message.Text, "Отмена", StringComparison.OrdinalIgnoreCase))
        {
            await _users.SetStateAsync(user.TelegramUserId, BotStates.Idle, payload: null);
            await bot.SendTextMessageAsync(user.TelegramUserId, "Ок, отменено.", replyMarkup: BotKeyboards.MainMenu());
            return;
        }

        // Главное меню кнопками
        if (state.State == BotStates.Idle)
        {
            await HandleMainMenuAsync(bot, user, message.Text);
            return;
        }

        // Ввод страны/города
        if (state.State == BotStates.AwaitCountry)
        {
            var country = (message.Text ?? "").Trim();
            if (country.Length < 2)
            {
                await bot.SendTextMessageAsync(user.TelegramUserId, "Введите страну (минимум 2 символа).");
                return;
            }

            await _users.UpdateLocationAsync(user.TelegramUserId, country, city: null);
            await _users.SetStateAsync(user.TelegramUserId, BotStates.AwaitCity);

            await bot.SendTextMessageAsync(user.TelegramUserId, "Теперь введите город.");
            return;
        }

        if (state.State == BotStates.AwaitCity)
        {
            var city = (message.Text ?? "").Trim();
            if (city.Length < 2)
            {
                await bot.SendTextMessageAsync(user.TelegramUserId, "Введите город (минимум 2 символа).");
                return;
            }

            await _users.UpdateLocationAsync(user.TelegramUserId, country: null, city: city);
            await _users.SetStateAsync(user.TelegramUserId, BotStates.Idle);

            await bot.SendTextMessageAsync(user.TelegramUserId, "Место сохранено ✅", replyMarkup: BotKeyboards.ProfileMenu());
            return;
        }

        // Создание объявления: ввод полей
        if (state.State is BotStates.Creating_AwaitTitle or BotStates.Creating_AwaitText or BotStates.Creating_AwaitContacts or BotStates.Creating_AwaitMedia)
        {
            await HandleCreateFlowInputAsync(bot, user, state, message);
            return;
        }

        // Поиск: ввод ключевых слов
        if (state.State == BotStates.Searching_AwaitKeywords)
        {
            var ctx = await _users.GetStatePayloadAsync<FlowContext>(user.TelegramUserId) ?? new FlowContext();
            ctx.SearchKeywords = (message.Text ?? "").Trim();

            await _users.SetStateAsync(user.TelegramUserId, BotStates.Idle, payload: null);

            if (ctx.SelectedCategoryId == null)
            {
                await bot.SendTextMessageAsync(user.TelegramUserId, "Категория не выбрана.", replyMarkup: BotKeyboards.MainMenu());
                return;
            }

            var results = await _search.SearchAsync(ctx.SelectedCategoryId.Value, ctx.SearchKeywords, user.Country, user.City, take: 5);
            if (results.Count == 0)
            {
                await bot.SendTextMessageAsync(user.TelegramUserId, "Ничего не найдено.", replyMarkup: BotKeyboards.MainMenu());
                return;
            }

            await bot.SendTextMessageAsync(user.TelegramUserId, $"Найдено объявлений: {results.Count}");

            foreach (var ad in results)
            {
                var cat = await _categories.GetByIdAsync(ad.CategoryId);
                var snippet = ad.Text.Length > 350 ? ad.Text[..350] + "..." : ad.Text;

                var msgText = $"<b>{Escape(ad.Title)}</b>\n" +
                              $"{Escape(snippet)}\n\n" +
                              $"<b>Где:</b> {Escape(ad.Country)}, {Escape(ad.City)}\n" +
                              $"<b>Категория:</b> {Escape(cat?.Name ?? "")}";

                var kb = new InlineKeyboardMarkup(new[]
                {
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData("Перейти к объявлению", $"search:links:{ad.Id}"),
                        InlineKeyboardButton.WithCallbackData("Посмотреть контакт", $"search:contacts:{ad.Id}")
                    }
                });

                await bot.SendTextMessageAsync(user.TelegramUserId, msgText, parseMode: ParseMode.Html, replyMarkup: kb);
            }

            return;
        }

        // Если попали сюда — неизвестный ввод
        await bot.SendTextMessageAsync(user.TelegramUserId, "Не понял. Откройте меню.", replyMarkup: BotKeyboards.MainMenu());
        await _users.SetStateAsync(user.TelegramUserId, BotStates.Idle);
    }

    private async Task HandleChannelPostAsync(ITelegramBotClient bot, Message message, CancellationToken ct, bool isEdited)
    {
        await LogIncomingMessageAsync(bot, message, isEdited, ct);
    }

    private async Task LogIncomingMessageAsync(ITelegramBotClient bot, Message message, bool isEdited, CancellationToken ct)
    {
        var botUserId = bot.BotId;
        var botEntity = await _db.TelegramBots.AsNoTracking()
            .FirstOrDefaultAsync(x => x.TelegramUserId == botUserId, ct);

        if (botEntity == null || !botEntity.TrackMessagesEnabled)
            return;

        var forwardFromUserId = message.ForwardFrom?.Id;
        var forwardFromChatId = message.ForwardFromChat?.Id;
        var log = new TelegramMessageLog
        {
            TelegramBotId = botEntity.Id,
            TelegramBotUserId = botUserId,
            ChatId = message.Chat.Id,
            ChatType = message.Chat.Type.ToString(),
            ChatTitle = message.Chat.Title,
            MessageId = message.MessageId,
            MessageDateUtc = message.Date.ToUniversalTime(),
            FromUserId = message.From?.Id,
            FromUsername = message.From?.Username,
            FromFirstName = message.From?.FirstName,
            Text = message.Text,
            Caption = message.Caption,
            IsForwarded = forwardFromUserId.HasValue || forwardFromChatId.HasValue,
            ForwardFromUserId = forwardFromUserId,
            ForwardFromChatId = forwardFromChatId,
            PayloadJson = JsonSerializer.Serialize(new
            {
                message,
                isEdited
            })
        };

        _db.TelegramMessageLogs.Add(log);
        await _db.SaveChangesAsync(ct);
    }

    private async Task HandleStartAsync(ITelegramBotClient bot, AppUser user, string text)
    {
        // /start <payload>
        var parts = text.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var payload = parts.Length > 1 ? parts[1].Trim() : null;

        // Реферал: ref_<code>
        if (!string.IsNullOrWhiteSpace(payload) && payload.StartsWith("ref_", StringComparison.OrdinalIgnoreCase))
        {
            var code = payload.Substring("ref_".Length);
            await _users.TryAttachReferrerAsync(user.TelegramUserId, code);
        }

        if (string.Equals(payload, "publish", StringComparison.OrdinalIgnoreCase))
        {
            await StartCreateFlowAsync(bot, user);
            return;
        }

        var hello = new StringBuilder();
        hello.AppendLine("Привет! Это бот объявлений для швейной индустрии.");
        hello.AppendLine();
        hello.AppendLine("• Бесплатно: без фото/видео и без ссылок, 1 раз в сутки.");
        hello.AppendLine("• Платно: можно фото/видео и ссылки, плюс платное поднятие.");
        hello.AppendLine();
        hello.AppendLine("Выберите действие в меню ниже.");

        await _users.SetStateAsync(user.TelegramUserId, BotStates.Idle);
        await bot.SendTextMessageAsync(user.TelegramUserId, hello.ToString(), replyMarkup: BotKeyboards.MainMenu());
    }

    private async Task HandleMainMenuAsync(ITelegramBotClient bot, AppUser user, string? text)
    {
        text ??= string.Empty;

        switch (text)
        {
            case "Создать объявление":
                await StartCreateFlowAsync(bot, user);
                return;

            case "Найти объявление":
                await StartSearchFlowAsync(bot, user);
                return;

            case "Мой профиль":
                await ShowProfileAsync(bot, user);
                return;

            case "Помощь":
                await ShowHelpAsync(bot, user);
                return;


            case "Место":
                await _users.SetStateAsync(user.TelegramUserId, BotStates.AwaitCountry);
                await bot.SendTextMessageAsync(user.TelegramUserId, "Введите страну:");
                return;

            case "Мои объявления":
                await ShowMyAdsAsync(bot, user);
                return;

            case "Реферальная ссылка":
                await SendReferralLinkAsync(bot, user);
                return;

            case "Назад":
                await bot.SendTextMessageAsync(user.TelegramUserId, "Главное меню:", replyMarkup: BotKeyboards.MainMenu());
                return;

            case "Платное объявление":
                var tariffsUrl = await GetTariffsUrlAsync();
                await bot.SendTextMessageAsync(user.TelegramUserId,
                    $"Тарифы на платные размещения: {tariffsUrl}\n\n" +
                    "Чтобы разместить платное объявление, нажмите «Создать объявление» и выберите «Платное».");
                return;

            default:
                await bot.SendTextMessageAsync(user.TelegramUserId, "Выберите действие из меню.", replyMarkup: BotKeyboards.MainMenu());
                return;
        }
    }

    private async Task ShowProfileAsync(ITelegramBotClient bot, AppUser user)
    {
        var location = (!string.IsNullOrWhiteSpace(user.Country) && !string.IsNullOrWhiteSpace(user.City))
            ? $"{user.Country}, {user.City}"
            : "не задано";

        var refLink = $"ref_{user.ReferralCode}";

        var text = $"<b>Профиль</b>\n" +
                   $"Место: <b>{Escape(location)}</b>\n" +
                   $"Реф.код: <code>{Escape(refLink)}</code>\n" +
                   $"Баланс: <b>{user.Balance:0.00}</b>";

        await _users.SetStateAsync(user.TelegramUserId, BotStates.Idle);
        await bot.SendTextMessageAsync(user.TelegramUserId, text, parseMode: ParseMode.Html, replyMarkup: BotKeyboards.ProfileMenu());
    }

    private async Task ShowHelpAsync(ITelegramBotClient bot, AppUser user)
    {
        var tariffsUrl = await GetTariffsUrlAsync();
        var text = "Правила:\n" +
                   "• Бесплатные объявления: 1 раз в сутки, без фото/видео и без ссылок.\n" +
                   "• Контакты: только @username, телефон или email.\n" +
                   "• Платные объявления: можно фото/видео и ссылки.\n\n" +
                   $"Тарифы: {tariffsUrl}";

        await bot.SendTextMessageAsync(user.TelegramUserId, text, replyMarkup: BotKeyboards.MainMenu());
    }

    private async Task StartCreateFlowAsync(ITelegramBotClient bot, AppUser user)
    {
        if (string.IsNullOrWhiteSpace(user.Country) || string.IsNullOrWhiteSpace(user.City))
        {
            await _users.SetStateAsync(user.TelegramUserId, BotStates.AwaitCountry);
            await bot.SendTextMessageAsync(user.TelegramUserId, "Сначала укажите страну. (Профиль → Место)\nВведите страну:");
            return;
        }

        var roots = await _categories.GetRootAsync();
        await _users.SetStateAsync(user.TelegramUserId, BotStates.Creating_SelectCategory, new FlowContext { CategoryParentId = null });

        await bot.SendTextMessageAsync(user.TelegramUserId, "Выберите категорию:", replyMarkup: BotKeyboards.Categories(roots));
    }

    private async Task StartSearchFlowAsync(ITelegramBotClient bot, AppUser user)
    {
        var roots = await _categories.GetRootAsync();
        await _users.SetStateAsync(user.TelegramUserId, BotStates.Searching_SelectCategory, new FlowContext { CategoryParentId = null });

        await bot.SendTextMessageAsync(user.TelegramUserId, "Выберите категорию для поиска:", replyMarkup: BotKeyboards.Categories(roots));
    }

    private async Task HandleCallbackAsync(ITelegramBotClient bot, CallbackQuery cq, CancellationToken ct)
    {
        if (cq.From == null)
            return;

        var user = await _users.GetOrCreateAsync(cq.From.Id, cq.From.Username, cq.From.FirstName);
        var state = await _users.GetOrCreateStateAsync(user.TelegramUserId);
        var data = cq.Data ?? string.Empty;

        // Модерация
        if (data.StartsWith("mod:", StringComparison.Ordinal))
        {
            await HandleModerationCallbackAsync(bot, cq, user, data);
            return;
        }

        // Категории
        if (data.StartsWith("cat:", StringComparison.Ordinal))
        {
            await HandleCategoryCallbackAsync(bot, cq, user, state, data);
            return;
        }

        if (data.StartsWith("catback:", StringComparison.Ordinal))
        {
            await HandleCategoryBackCallbackAsync(bot, cq, user, state, data);
            return;
        }

        // Выбор типа
        if (data == "type:free" || data == "type:paid")
        {
            await HandleTypeCallbackAsync(bot, cq, user, state, data);
            return;
        }

        // Предпросмотр: publish/pay/edit/cancel
        if (data.StartsWith("create:", StringComparison.Ordinal))
        {
            await HandleCreateCallbackAsync(bot, cq, user, data);
            return;
        }

        // Поиск: ссылки/контакты
        if (data.StartsWith("search:", StringComparison.Ordinal))
        {
            await HandleSearchCallbackAsync(bot, cq, user, data);
            return;
        }

        // Мои объявления
        if (data.StartsWith("myad:", StringComparison.Ordinal))
        {
            await HandleMyAdsCallbackAsync(bot, cq, user, data);
            return;
        }

        await bot.AnswerCallbackQueryAsync(cq.Id);
    }

    private async Task HandleCategoryCallbackAsync(ITelegramBotClient bot, CallbackQuery cq, AppUser user, UserState state, string data)
    {
        var idStr = data.Split(':', 2)[1];
        if (!Guid.TryParse(idStr, out var catId))
        {
            await bot.AnswerCallbackQueryAsync(cq.Id, "Некорректная категория.");
            return;
        }

        var hasChildren = await _categories.HasChildrenAsync(catId);
        if (hasChildren)
        {
            var children = await _categories.GetChildrenAsync(catId);

            // back callback
            var cat = await _categories.GetByIdAsync(catId);
            var backData = cat?.ParentId == null ? "catback:root" : $"catback:{cat.ParentId}";

            await bot.EditMessageTextAsync(
                chatId: cq.Message!.Chat.Id,
                messageId: cq.Message!.MessageId,
                text: "Выберите подкатегорию:",
                replyMarkup: BotKeyboards.Categories(children, backData));

            // сохраняем parentId
            var ctx = await _users.GetStatePayloadAsync<FlowContext>(user.TelegramUserId) ?? new FlowContext();
            ctx.CategoryParentId = catId;
            await _users.SetStateAsync(user.TelegramUserId, state.State, ctx);

            await bot.AnswerCallbackQueryAsync(cq.Id);
            return;
        }

        // leaf выбран
        var ctx2 = await _users.GetStatePayloadAsync<FlowContext>(user.TelegramUserId) ?? new FlowContext();
        ctx2.SelectedCategoryId = catId;
        await _users.SetStateAsync(user.TelegramUserId, state.State, ctx2);

        if (state.State == BotStates.Creating_SelectCategory)
        {
            await _users.SetStateAsync(user.TelegramUserId, BotStates.Creating_SelectType, ctx2);
            await bot.EditMessageTextAsync(
                chatId: cq.Message!.Chat.Id,
                messageId: cq.Message!.MessageId,
                text: "Выберите тип объявления:",
                replyMarkup: BotKeyboards.AdType());
        }
        else if (state.State == BotStates.Searching_SelectCategory)
        {
            await _users.SetStateAsync(user.TelegramUserId, BotStates.Searching_AwaitKeywords, ctx2);
            await bot.SendTextMessageAsync(user.TelegramUserId, "Введите ключевые слова для поиска (или отправьте «-» чтобы искать без слов):");
        }

        await bot.AnswerCallbackQueryAsync(cq.Id);
    }

    private async Task HandleCategoryBackCallbackAsync(ITelegramBotClient bot, CallbackQuery cq, AppUser user, UserState state, string data)
    {
        string target = data.Split(':', 2)[1];

        List<Category> cats;
        string? back = null;

        if (target == "root")
        {
            cats = await _categories.GetRootAsync();
        }
        else if (Guid.TryParse(target, out var parentId))
        {
            cats = await _categories.GetChildrenAsync(parentId);
            var parent = await _categories.GetByIdAsync(parentId);
            back = parent?.ParentId == null ? "catback:root" : $"catback:{parent.ParentId}";
        }
        else
        {
            cats = await _categories.GetRootAsync();
        }

        await bot.EditMessageTextAsync(
            chatId: cq.Message!.Chat.Id,
            messageId: cq.Message!.MessageId,
            text: "Выберите категорию:",
            replyMarkup: BotKeyboards.Categories(cats, back));

        await bot.AnswerCallbackQueryAsync(cq.Id);
    }

    private async Task HandleTypeCallbackAsync(ITelegramBotClient bot, CallbackQuery cq, AppUser user, UserState state, string data)
    {
        var ctx = await _users.GetStatePayloadAsync<FlowContext>(user.TelegramUserId) ?? new FlowContext();
        if (ctx.SelectedCategoryId == null)
        {
            await bot.AnswerCallbackQueryAsync(cq.Id, "Сначала выберите категорию.");
            return;
        }

        var isPaid = data == "type:paid";

        try
        {
            var draft = await _ads.CreateDraftAsync(user, ctx.SelectedCategoryId.Value, isPaid);
            ctx.DraftAdId = draft.Id;

            await _users.SetStateAsync(user.TelegramUserId, BotStates.Creating_AwaitTitle, ctx);

            var text = isPaid
                ? $"Платное объявление ✅\nТарифы: {_appOptions.TelegraphTariffsUrl}\n\nВведите заголовок объявления:"
                : "Введите заголовок объявления:";

            await bot.SendTextMessageAsync(user.TelegramUserId, text);
        }
        catch (Exception ex)
        {
            await bot.SendTextMessageAsync(user.TelegramUserId, ex.Message);
        }

        await bot.AnswerCallbackQueryAsync(cq.Id);
    }

    private async Task HandleCreateFlowInputAsync(ITelegramBotClient bot, AppUser user, UserState state, Message message)
    {
        var ctx = await _users.GetStatePayloadAsync<FlowContext>(user.TelegramUserId) ?? new FlowContext();
        if (ctx.DraftAdId == null)
        {
            await _users.SetStateAsync(user.TelegramUserId, BotStates.Idle);
            await bot.SendTextMessageAsync(user.TelegramUserId, "Черновик не найден. Начните заново.", replyMarkup: BotKeyboards.MainMenu());
            return;
        }

        var ad = await _ads.GetByIdAsync(ctx.DraftAdId.Value);
        if (ad == null)
        {
            await _users.SetStateAsync(user.TelegramUserId, BotStates.Idle);
            await bot.SendTextMessageAsync(user.TelegramUserId, "Черновик не найден. Начните заново.", replyMarkup: BotKeyboards.MainMenu());
            return;
        }

        if (state.State == BotStates.Creating_AwaitTitle)
        {
            var (ok, err) = await _ads.SetTitleAsync(ad.Id, message.Text ?? "");
            if (!ok)
            {
                await bot.SendTextMessageAsync(user.TelegramUserId, err ?? "Ошибка.");
                return;
            }

            await _users.SetStateAsync(user.TelegramUserId, BotStates.Creating_AwaitText, ctx);
            await bot.SendTextMessageAsync(user.TelegramUserId, "Введите текст объявления:");
            return;
        }

        if (state.State == BotStates.Creating_AwaitText)
        {
            var (ok, err) = await _ads.SetTextAsync(ad.Id, message.Text ?? "");
            if (!ok)
            {
                await bot.SendTextMessageAsync(user.TelegramUserId, err ?? "Ошибка.");
                return;
            }

            await _users.SetStateAsync(user.TelegramUserId, BotStates.Creating_AwaitContacts, ctx);
            await bot.SendTextMessageAsync(user.TelegramUserId, "Введите контакты (только @username, телефон или email):");
            return;
        }

        if (state.State == BotStates.Creating_AwaitContacts)
        {
            var (ok, err) = await _ads.SetContactsAsync(ad.Id, message.Text ?? "");
            if (!ok)
            {
                await bot.SendTextMessageAsync(user.TelegramUserId, err ?? "Ошибка.");
                return;
            }

            if (ad.IsPaid)
            {
                await _users.SetStateAsync(user.TelegramUserId, BotStates.Creating_AwaitMedia, ctx);
                await bot.SendTextMessageAsync(user.TelegramUserId, "Отправьте фото или видео (или нажмите «Пропустить»).", replyMarkup: BotKeyboards.SkipMedia());
            }
            else
            {
                await ShowPreviewAsync(bot, user, ad.Id);
            }

            return;
        }

        if (state.State == BotStates.Creating_AwaitMedia)
        {
            // Пропуск
            if (string.Equals(message.Text, "Пропустить", StringComparison.OrdinalIgnoreCase))
            {
                await ShowPreviewAsync(bot, user, ad.Id);
                return;
            }

            if (message.Photo != null && message.Photo.Length > 0)
            {
                var fileId = message.Photo[^1].FileId;
                await _ads.SetMediaAsync(ad.Id, MediaType.Photo, fileId);
                await ShowPreviewAsync(bot, user, ad.Id);
                return;
            }

            if (message.Video != null)
            {
                await _ads.SetMediaAsync(ad.Id, MediaType.Video, message.Video.FileId);
                await ShowPreviewAsync(bot, user, ad.Id);
                return;
            }

            await bot.SendTextMessageAsync(user.TelegramUserId, "Отправьте фото/видео или нажмите «Пропустить».");
        }
    }

    private async Task ShowPreviewAsync(ITelegramBotClient bot, AppUser user, Guid adId)
    {
        var ad = await _ads.GetByIdAsync(adId);
        if (ad == null)
            return;

        var category = await _categories.GetByIdAsync(ad.CategoryId);
        if (category == null)
            return;

        // Для предпросмотра используем "виртуальный канал" с дефолтным футером.
        var defaultFooterText = (await _settings.GetAsync("App.DefaultFooterLinkText"))?.Trim();
        var defaultFooterUrl = (await _settings.GetAsync("App.DefaultFooterLinkUrl"))?.Trim();
        if (string.IsNullOrWhiteSpace(defaultFooterText))
            defaultFooterText = _appOptions.DefaultFooterLinkText;
        if (string.IsNullOrWhiteSpace(defaultFooterUrl))
            defaultFooterUrl = _appOptions.DefaultFooterLinkUrl;

        var previewChannel = new Channel
        {
            Title = "Preview",
            TelegramChatId = 0,
            TelegramUsername = null,
            FooterLinkText = defaultFooterText,
            FooterLinkUrl = defaultFooterUrl
        };

        var text = _formatter.BuildPostText(ad, category, previewChannel);

        await _users.SetStateAsync(user.TelegramUserId, BotStates.Creating_Preview, new FlowContext { DraftAdId = adId });

        var kb = ad.IsPaid ? BotKeyboards.PreviewPaid(adId) : BotKeyboards.PreviewFree(adId);

        if (ad.IsPaid && ad.MediaType != MediaType.None && !string.IsNullOrWhiteSpace(ad.MediaFileId))
        {
            // В предпросмотре отправляем как новый месседж (чтобы не возиться с edit media)
            if (ad.MediaType == MediaType.Photo)
                await bot.SendPhotoAsync(user.TelegramUserId, InputFile.FromFileId(ad.MediaFileId), caption: text, parseMode: ParseMode.Html, replyMarkup: kb);
            else
                await bot.SendVideoAsync(user.TelegramUserId, InputFile.FromFileId(ad.MediaFileId), caption: text, parseMode: ParseMode.Html, replyMarkup: kb);
        }
        else
        {
            await bot.SendTextMessageAsync(user.TelegramUserId, text, parseMode: ParseMode.Html, replyMarkup: kb);
        }
    }

    private async Task ShowMyAdsAsync(ITelegramBotClient bot, AppUser user)
    {
        var ads = await _ads.GetUserAdsAsync(user.Id, take: 10);
        if (ads.Count == 0)
        {
            await bot.SendTextMessageAsync(user.TelegramUserId, "У вас пока нет объявлений.", replyMarkup: BotKeyboards.MainMenu());
            return;
        }

        await bot.SendTextMessageAsync(user.TelegramUserId, "Ваши объявления (последние 10):");

        foreach (var ad in ads)
        {
            var category = await _categories.GetByIdAsync(ad.CategoryId);
            var title = string.IsNullOrWhiteSpace(ad.Title) ? "Без заголовка" : ad.Title;
            var statusText = GetAdStatusLabel(ad.Status);
            var typeText = ad.IsPaid ? "Платное" : "Бесплатное";

            var text = $"<b>{Escape(title)}</b>\n" +
                       $"Статус: <b>{statusText}</b>\n" +
                       $"Тип: {typeText}\n" +
                       $"Категория: {Escape(category?.Name ?? "—")}\n" +
                       $"Создано: {ad.CreatedAtUtc:yyyy-MM-dd HH:mm} UTC";

            var rows = new List<List<InlineKeyboardButton>>
            {
                new()
                {
                    InlineKeyboardButton.WithCallbackData("Подробнее", $"myad:view:{ad.Id}")
                }
            };

            if (ad.Status == AdStatus.Published)
            {
                var publishedRow = new List<InlineKeyboardButton>
                {
                    InlineKeyboardButton.WithCallbackData("Ссылки", $"myad:links:{ad.Id}")
                };

                if (ad.IsPaid)
                    publishedRow.Add(InlineKeyboardButton.WithCallbackData("Поднять", $"myad:bump:{ad.Id}"));

                rows.Add(publishedRow);
            }

            await bot.SendTextMessageAsync(user.TelegramUserId, text, parseMode: ParseMode.Html, replyMarkup: new InlineKeyboardMarkup(rows));
        }
    }

    private async Task HandleMyAdsCallbackAsync(ITelegramBotClient bot, CallbackQuery cq, AppUser user, string data)
    {
        var parts = data.Split(':', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 3)
        {
            await bot.AnswerCallbackQueryAsync(cq.Id);
            return;
        }

        var action = parts[1];
        if (!Guid.TryParse(parts[2], out var adId))
        {
            await bot.AnswerCallbackQueryAsync(cq.Id, "Некорректный ID.");
            return;
        }

        var ad = await _db.Ads.FirstOrDefaultAsync(x => x.Id == adId && x.UserId == user.Id);
        if (ad == null)
        {
            await bot.AnswerCallbackQueryAsync(cq.Id, "Объявление не найдено.");
            return;
        }

        if (action == "view")
        {
            var category = await _categories.GetByIdAsync(ad.CategoryId);
            var statusText = GetAdStatusLabel(ad.Status);
            var typeText = ad.IsPaid ? "Платное" : "Бесплатное";

            var text = $"<b>{Escape(ad.Title)}</b>\n" +
                       $"{Escape(ad.Text)}\n\n" +
                       $"Контакты: {Escape(ad.Contacts)}\n" +
                       $"Статус: <b>{statusText}</b>\n" +
                       $"Тип: {typeText}\n" +
                       $"Категория: {Escape(category?.Name ?? "—")}\n" +
                       $"Создано: {ad.CreatedAtUtc:yyyy-MM-dd HH:mm} UTC\n" +
                       $"Обновлено: {ad.UpdatedAtUtc:yyyy-MM-dd HH:mm} UTC";

            await bot.SendTextMessageAsync(user.TelegramUserId, text, parseMode: ParseMode.Html);
            await bot.AnswerCallbackQueryAsync(cq.Id);
            return;
        }

        if (action == "links")
        {
            if (ad.Status != AdStatus.Published)
            {
                await bot.AnswerCallbackQueryAsync(cq.Id, "Ссылки доступны только для опубликованных объявлений.");
                return;
            }

            var pubs = await _db.AdPublications
                .Where(x => x.AdId == adId)
                .OrderByDescending(x => x.PublishedAtUtc)
                .ToListAsync();

            if (pubs.Count == 0)
            {
                await bot.AnswerCallbackQueryAsync(cq.Id, "Ссылки недоступны.");
                return;
            }

            var channels = await _db.Channels.ToListAsync();
            var links = new List<string>();

            foreach (var p in pubs)
            {
                var ch = channels.FirstOrDefault(x => x.Id == p.ChannelId);
                if (ch?.TelegramUsername != null)
                    links.Add($"https://t.me/{ch.TelegramUsername.TrimStart('@')}/{p.TelegramMessageId}");
            }

            if (links.Count == 0)
            {
                await bot.SendTextMessageAsync(user.TelegramUserId, "Ссылки не удалось сформировать (каналы без username).");
            }
            else
            {
                await bot.SendTextMessageAsync(user.TelegramUserId, "Ссылки:\n" + string.Join("\n", links));
            }

            await bot.AnswerCallbackQueryAsync(cq.Id);
            return;
        }

        if (action == "bump")
        {
            if (!ad.IsPaid)
            {
                await bot.AnswerCallbackQueryAsync(cq.Id, "Поднятие доступно только для платных объявлений.");
                return;
            }

            if (ad.Status != AdStatus.Published)
            {
                await bot.AnswerCallbackQueryAsync(cq.Id, "Поднять можно только опубликованные объявления.");
                return;
            }

            await _payments.SendBumpInvoiceAsync(user.TelegramUserId, ad.Id);
            await bot.SendTextMessageAsync(user.TelegramUserId, "Счёт на поднятие отправлен ✅");
            await bot.AnswerCallbackQueryAsync(cq.Id);
            return;
        }

        await bot.AnswerCallbackQueryAsync(cq.Id);
    }

    private async Task SendReferralLinkAsync(ITelegramBotClient bot, AppUser user)
    {
        var enabled = await _settings.GetBoolAsync("App.EnableReferralProgram", _appOptions.EnableReferralProgram);
        if (!enabled)
        {
            await bot.SendTextMessageAsync(user.TelegramUserId, "Реферальная программа сейчас отключена.");
            return;
        }

        var me = await bot.GetMeAsync();
        if (string.IsNullOrWhiteSpace(me.Username))
        {
            await bot.SendTextMessageAsync(user.TelegramUserId, "Не удалось определить username бота.");
            return;
        }

        var refLink = $"https://t.me/{me.Username}?start=ref_{user.ReferralCode}";
        var text = "Ваша реферальная ссылка:\n" +
                   $"{refLink}\n\n" +
                   "Поделитесь ею, чтобы получать бонусы за оплаты привлечённых пользователей.";

        await bot.SendTextMessageAsync(user.TelegramUserId, text);
    }

    private async Task HandleCreateCallbackAsync(ITelegramBotClient bot, CallbackQuery cq, AppUser user, string data)
    {
        var parts = data.Split(':', StringSplitOptions.RemoveEmptyEntries);

        // create:cancel
        if (parts.Length == 2 && parts[1] == "cancel")
        {
            await _users.SetStateAsync(user.TelegramUserId, BotStates.Idle, payload: null);
            await bot.SendTextMessageAsync(user.TelegramUserId, "Ок, отменено.", replyMarkup: BotKeyboards.MainMenu());
            await bot.AnswerCallbackQueryAsync(cq.Id);
            return;
        }

        // create:edit:<field>:<adId>
        if (parts.Length == 4 && parts[1] == "edit")
        {
            var field = parts[2];
            if (!Guid.TryParse(parts[3], out var adId))
            {
                await bot.AnswerCallbackQueryAsync(cq.Id, "Некорректный ID.");
                return;
            }

            var ctx = new FlowContext { DraftAdId = adId, EditField = field };
            await _users.SetStateAsync(user.TelegramUserId, BotStates.Creating_AwaitTitle, ctx); // временно, ниже поправим

            switch (field)
            {
                case "title":
                    await _users.SetStateAsync(user.TelegramUserId, BotStates.Creating_AwaitTitle, ctx);
                    await bot.SendTextMessageAsync(user.TelegramUserId, "Введите новый заголовок:");
                    break;

                case "text":
                    await _users.SetStateAsync(user.TelegramUserId, BotStates.Creating_AwaitText, ctx);
                    await bot.SendTextMessageAsync(user.TelegramUserId, "Введите новый текст:");
                    break;

                case "contacts":
                    await _users.SetStateAsync(user.TelegramUserId, BotStates.Creating_AwaitContacts, ctx);
                    await bot.SendTextMessageAsync(user.TelegramUserId, "Введите новые контакты:");
                    break;

                case "media":
                    await _users.SetStateAsync(user.TelegramUserId, BotStates.Creating_AwaitMedia, ctx);
                    await bot.SendTextMessageAsync(user.TelegramUserId, "Отправьте новое фото/видео (или «Пропустить»).", replyMarkup: BotKeyboards.SkipMedia());
                    break;

                default:
                    await bot.SendTextMessageAsync(user.TelegramUserId, "Неизвестное поле.");
                    break;
            }

            await bot.AnswerCallbackQueryAsync(cq.Id);
            return;
        }

        // create:publish:<adId> (free)
        if (parts.Length == 3 && parts[1] == "publish")
        {
            if (!Guid.TryParse(parts[2], out var adId))
            {
                await bot.AnswerCallbackQueryAsync(cq.Id, "Некорректный ID.");
                return;
            }

            var pub = await _publication.SubmitAsync(user.TelegramUserId, adId);

            if (pub.Ok)
            {
                var text = pub.Message;
                if (pub.PublishedLinks.Count > 0)
                    text += "\n\nСсылки:\n" + string.Join("\n", pub.PublishedLinks);

                await bot.SendTextMessageAsync(user.TelegramUserId, text, replyMarkup: BotKeyboards.MainMenu());
            }
            else
            {
                await bot.SendTextMessageAsync(user.TelegramUserId, pub.Message, replyMarkup: BotKeyboards.MainMenu());
            }

            await _users.SetStateAsync(user.TelegramUserId, BotStates.Idle, payload: null);
            await bot.AnswerCallbackQueryAsync(cq.Id);
            return;
        }

        // create:pay:<adId> (paid -> invoice, auto publish after payment)
        if (parts.Length == 3 && parts[1] == "pay")
        {
            if (!Guid.TryParse(parts[2], out var adId))
            {
                await bot.AnswerCallbackQueryAsync(cq.Id, "Некорректный ID.");
                return;
            }

            await _payments.SendPaidAdInvoiceAsync(user.TelegramUserId, adId);

            await bot.SendTextMessageAsync(user.TelegramUserId,
                "Счёт на оплату отправлен ✅\nПосле оплаты объявление опубликуется автоматически.");

            await _users.SetStateAsync(user.TelegramUserId, BotStates.Idle, payload: null);
            await bot.AnswerCallbackQueryAsync(cq.Id);
            return;
        }

        await bot.AnswerCallbackQueryAsync(cq.Id);
    }

    private async Task HandleSearchCallbackAsync(ITelegramBotClient bot, CallbackQuery cq, AppUser user, string data)
    {
        var parts = data.Split(':', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 3)
        {
            await bot.AnswerCallbackQueryAsync(cq.Id);
            return;
        }

        var action = parts[1];
        if (!Guid.TryParse(parts[2], out var adId))
        {
            await bot.AnswerCallbackQueryAsync(cq.Id, "Некорректный ID.");
            return;
        }

        if (action == "contacts")
        {
            var ad = await _db.Ads.FirstOrDefaultAsync(x => x.Id == adId && x.Status == AdStatus.Published);
            if (ad == null)
            {
                await bot.AnswerCallbackQueryAsync(cq.Id, "Объявление не найдено.");
                return;
            }

            await bot.SendTextMessageAsync(user.TelegramUserId, $"Контакты автора:\n{ad.Contacts}");
            await bot.AnswerCallbackQueryAsync(cq.Id);
            return;
        }

        if (action == "links")
        {
            var pubs = await _db.AdPublications
                .Where(x => x.AdId == adId)
                .OrderByDescending(x => x.PublishedAtUtc)
                .ToListAsync();

            if (pubs.Count == 0)
            {
                await bot.AnswerCallbackQueryAsync(cq.Id, "Ссылки недоступны.");
                return;
            }

            var channels = await _db.Channels.ToListAsync();

            var links = new List<string>();
            foreach (var p in pubs)
            {
                var ch = channels.FirstOrDefault(x => x.Id == p.ChannelId);
                if (ch?.TelegramUsername != null)
                    links.Add($"https://t.me/{ch.TelegramUsername.TrimStart('@')}/{p.TelegramMessageId}");
            }

            if (links.Count == 0)
            {
                await bot.SendTextMessageAsync(user.TelegramUserId, "Ссылки не удалось сформировать (каналы без username).");
            }
            else
            {
                await bot.SendTextMessageAsync(user.TelegramUserId, "Ссылки:\n" + string.Join("\n", links));
            }

            await bot.AnswerCallbackQueryAsync(cq.Id);
            return;
        }

        await bot.AnswerCallbackQueryAsync(cq.Id);
    }

    private async Task HandleModerationCallbackAsync(ITelegramBotClient bot, CallbackQuery cq, AppUser user, string data)
    {
        // mod:approve:<id> | mod:reject:<id>
        var parts = data.Split(':', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 3)
        {
            await bot.AnswerCallbackQueryAsync(cq.Id);
            return;
        }

        var action = parts[1];
        if (!Guid.TryParse(parts[2], out var reqId))
        {
            await bot.AnswerCallbackQueryAsync(cq.Id, "Некорректный ID.");
            return;
        }

        if (action == "approve")
        {
            var (ok, msg) = await _moderation.ApproveAsync(reqId, user.TelegramUserId);
            await bot.AnswerCallbackQueryAsync(cq.Id, msg);
            return;
        }

        if (action == "reject")
        {
            var (ok, msg) = await _moderation.RejectAsync(reqId, user.TelegramUserId);
            await bot.AnswerCallbackQueryAsync(cq.Id, msg);
            return;
        }

        await bot.AnswerCallbackQueryAsync(cq.Id);
    }

    /// <summary>
    /// Получить ссылку на страницу с тарифами.
    /// Сначала читаем из AppSettings (ключ "App.TelegraphTariffsUrl"),
    /// а если там пусто — берём значение из appsettings.json.
    /// </summary>
    private async Task<string> GetTariffsUrlAsync()
    {
        var v = (await _settings.GetAsync("App.TelegraphTariffsUrl"))?.Trim();
        return string.IsNullOrWhiteSpace(v) ? _appOptions.TelegraphTariffsUrl : v;
    }

    private static string GetAdStatusLabel(AdStatus status) => status switch
    {
        AdStatus.Draft => "Черновик",
        AdStatus.PendingModeration => "На модерации",
        AdStatus.Published => "Опубликовано",
        AdStatus.Rejected => "Отклонено",
        _ => status.ToString()
    };

    private static string Escape(string? s)
        => System.Net.WebUtility.HtmlEncode(s ?? string.Empty);
}
