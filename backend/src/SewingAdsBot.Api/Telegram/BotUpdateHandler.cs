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
        var lang = BotTexts.Normalize(user.Language);

        // /start
        if (!string.IsNullOrWhiteSpace(message.Text) && message.Text.StartsWith("/start", StringComparison.OrdinalIgnoreCase))
        {
            await HandleStartAsync(bot, user, message.Text);
            return;
        }

        if (string.IsNullOrWhiteSpace(user.Language))
        {
            await _users.SetStateAsync(user.TelegramUserId, BotStates.ChoosingLanguage);
            await bot.SendTextMessageAsync(user.TelegramUserId,
                BotTexts.Text(lang, BotTextKeys.LanguageChooseTitle),
                replyMarkup: BotKeyboards.LanguageSelection());
            return;
        }

        var state = await _users.GetOrCreateStateAsync(user.TelegramUserId);

        // Глобальная отмена
        if (BotTexts.Matches(message.Text, lang, BotTextKeys.Cancel))
        {
            await _users.SetStateAsync(user.TelegramUserId, BotStates.Idle, payload: null);
            await bot.SendTextMessageAsync(user.TelegramUserId, BotTexts.Text(lang, BotTextKeys.Canceled), replyMarkup: BotKeyboards.MainMenu(lang));
            return;
        }

        // Главное меню кнопками
        if (state.State == BotStates.Idle)
        {
            await HandleMainMenuAsync(bot, user, message.Text);
            return;
        }

        if (state.State == BotStates.ChoosingLanguage)
        {
            await bot.SendTextMessageAsync(user.TelegramUserId,
                BotTexts.Text(lang, BotTextKeys.LanguageChooseTitle),
                replyMarkup: BotKeyboards.LanguageSelection());
            return;
        }

        // Ввод страны/города
        if (state.State == BotStates.AwaitCountry)
        {
            var country = (message.Text ?? "").Trim();
            var (mode, countries) = await GetLocationModeAsync();
            if (mode == LocationInputMode.Keyboard && countries.Count > 0)
            {
                if (!countries.Contains(country, StringComparer.OrdinalIgnoreCase))
                {
                    await bot.SendTextMessageAsync(user.TelegramUserId, BotTexts.Text(lang, BotTextKeys.InvalidLocationSelection));
                    return;
                }
            }
            else if (country.Length < 2)
            {
                await bot.SendTextMessageAsync(user.TelegramUserId, BotTexts.Text(lang, BotTextKeys.EnterCountryShort));
                return;
            }

            await _users.UpdateLocationAsync(user.TelegramUserId, country, city: null);
            await _users.SetStateAsync(user.TelegramUserId, BotStates.AwaitCity);

            var (cityMode, cities) = await GetCitiesAsync(country);
            if (cityMode == LocationInputMode.Keyboard && cities.Count > 0)
            {
                await bot.SendTextMessageAsync(user.TelegramUserId, BotTexts.Text(lang, BotTextKeys.SelectCity),
                    replyMarkup: BotKeyboards.LocationOptions(cities, lang));
            }
            else
            {
                await bot.SendTextMessageAsync(user.TelegramUserId, BotTexts.Text(lang, BotTextKeys.EnterCity));
            }
            return;
        }

        if (state.State == BotStates.AwaitCity)
        {
            var city = (message.Text ?? "").Trim();
            var (mode, cities) = await GetCitiesAsync(user.Country ?? string.Empty);
            if (mode == LocationInputMode.Keyboard && cities.Count > 0)
            {
                if (!cities.Contains(city, StringComparer.OrdinalIgnoreCase))
                {
                    await bot.SendTextMessageAsync(user.TelegramUserId, BotTexts.Text(lang, BotTextKeys.InvalidLocationSelection));
                    return;
                }
            }
            else if (city.Length < 2)
            {
                await bot.SendTextMessageAsync(user.TelegramUserId, BotTexts.Text(lang, BotTextKeys.EnterCityShort));
                return;
            }

            await _users.UpdateLocationAsync(user.TelegramUserId, country: null, city: city);
            await _users.SetStateAsync(user.TelegramUserId, BotStates.Idle);

            await bot.SendTextMessageAsync(user.TelegramUserId, BotTexts.Text(lang, BotTextKeys.LocationSaved),
                replyMarkup: BotKeyboards.ProfileMenu(lang));
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
                await bot.SendTextMessageAsync(user.TelegramUserId, BotTexts.Text(lang, BotTextKeys.CategoryNotSelected),
                    replyMarkup: BotKeyboards.MainMenu(lang));
                return;
            }

            var results = await _search.SearchAsync(ctx.SelectedCategoryId.Value, ctx.SearchKeywords, user.Country, user.City, take: 5);
            if (results.Count == 0)
            {
                await bot.SendTextMessageAsync(user.TelegramUserId, BotTexts.Text(lang, BotTextKeys.SearchNothingFound),
                    replyMarkup: BotKeyboards.MainMenu(lang));
                return;
            }

            await bot.SendTextMessageAsync(user.TelegramUserId,
                string.Format(BotTexts.Text(lang, BotTextKeys.SearchResultsCount), results.Count));

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
                        InlineKeyboardButton.WithCallbackData(BotTexts.Text(lang, BotTextKeys.SearchGoToAd), $"search:links:{ad.Id}"),
                        InlineKeyboardButton.WithCallbackData(BotTexts.Text(lang, BotTextKeys.SearchViewContact), $"search:contacts:{ad.Id}")
                    }
                });

                await bot.SendTextMessageAsync(user.TelegramUserId, msgText, parseMode: ParseMode.Html, replyMarkup: kb);
            }

            return;
        }

        // Если попали сюда — неизвестный ввод
        await bot.SendTextMessageAsync(user.TelegramUserId, BotTexts.Text(lang, BotTextKeys.NotUnderstood),
            replyMarkup: BotKeyboards.MainMenu(lang));
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
        var lang = BotTexts.Normalize(user.Language);

        // /start <payload>
        var parts = text.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var payload = parts.Length > 1 ? parts[1].Trim() : null;

        // Реферал: ref_<code>
        if (!string.IsNullOrWhiteSpace(payload) && payload.StartsWith("ref_", StringComparison.OrdinalIgnoreCase))
        {
            var code = payload.Substring("ref_".Length);
            await _users.TryAttachReferrerAsync(user.TelegramUserId, code);
        }

        if (string.IsNullOrWhiteSpace(user.Language))
        {
            await _users.SetStateAsync(user.TelegramUserId, BotStates.ChoosingLanguage);
            await bot.SendTextMessageAsync(user.TelegramUserId,
                BotTexts.Text(lang, BotTextKeys.LanguageChooseTitle),
                replyMarkup: BotKeyboards.LanguageSelection());
            return;
        }

        if (string.Equals(payload, "publish", StringComparison.OrdinalIgnoreCase))
        {
            await StartCreateFlowAsync(bot, user);
            return;
        }

        await _users.SetStateAsync(user.TelegramUserId, BotStates.Idle);
        await bot.SendTextMessageAsync(user.TelegramUserId, BotTexts.Text(lang, BotTextKeys.StartGreeting),
            replyMarkup: BotKeyboards.MainMenu(lang));
    }

    private async Task HandleMainMenuAsync(ITelegramBotClient bot, AppUser user, string? text)
    {
        text ??= string.Empty;
        var lang = BotTexts.Normalize(user.Language);

        if (BotTexts.Matches(text, lang, BotTextKeys.MenuCreateAd))
        {
            await StartCreateFlowAsync(bot, user);
            return;
        }

        if (BotTexts.Matches(text, lang, BotTextKeys.MenuSearchAd))
        {
            await StartSearchFlowAsync(bot, user);
            return;
        }

        if (BotTexts.Matches(text, lang, BotTextKeys.MenuProfile))
        {
            await ShowProfileAsync(bot, user);
            return;
        }

        if (BotTexts.Matches(text, lang, BotTextKeys.MenuHelp))
        {
            await ShowHelpAsync(bot, user);
            return;
        }

        if (BotTexts.Matches(text, lang, BotTextKeys.MenuLocation))
        {
            await _users.SetStateAsync(user.TelegramUserId, BotStates.AwaitCountry);
            await SendCountryPromptAsync(bot, user);
            return;
        }

        if (BotTexts.Matches(text, lang, BotTextKeys.MenuMyAds))
        {
            await ShowMyAdsAsync(bot, user);
            return;
        }

        if (BotTexts.Matches(text, lang, BotTextKeys.MenuReferral))
        {
            await SendReferralLinkAsync(bot, user);
            return;
        }

        if (BotTexts.Matches(text, lang, BotTextKeys.MenuBack))
        {
            await bot.SendTextMessageAsync(user.TelegramUserId, BotTexts.Text(lang, BotTextKeys.MainMenuTitle),
                replyMarkup: BotKeyboards.MainMenu(lang));
            return;
        }

        if (BotTexts.Matches(text, lang, BotTextKeys.MenuPaidAd))
        {
            var tariffsUrl = await GetTariffsUrlAsync();
            var message = $"{BotTexts.Text(lang, BotTextKeys.PaidAdInfoTitle)} {tariffsUrl}\n\n" +
                          BotTexts.Text(lang, BotTextKeys.PaidAdInfo);
            var kb = new InlineKeyboardMarkup(
                InlineKeyboardButton.WithUrl(BotTexts.Text(lang, BotTextKeys.PaidTariffs), tariffsUrl)
            );
            await bot.SendTextMessageAsync(user.TelegramUserId, message, replyMarkup: kb);
            return;
        }

        await bot.SendTextMessageAsync(user.TelegramUserId, BotTexts.Text(lang, BotTextKeys.ChooseActionMenu),
            replyMarkup: BotKeyboards.MainMenu(lang));
    }

    private async Task ShowProfileAsync(ITelegramBotClient bot, AppUser user)
    {
        var lang = BotTexts.Normalize(user.Language);
        var location = (!string.IsNullOrWhiteSpace(user.Country) && !string.IsNullOrWhiteSpace(user.City))
            ? $"{user.Country}, {user.City}"
            : BotTexts.Text(lang, BotTextKeys.LocationNotSet);

        var refLink = $"ref_{user.ReferralCode}";

        var text = $"<b>{BotTexts.Text(lang, BotTextKeys.ProfileTitle)}</b>\n" +
                   $"{BotTexts.Text(lang, BotTextKeys.ProfileLocation)}: <b>{Escape(location)}</b>\n" +
                   $"{BotTexts.Text(lang, BotTextKeys.ProfileReferral)}: <code>{Escape(refLink)}</code>\n" +
                   $"{BotTexts.Text(lang, BotTextKeys.ProfileBalance)}: <b>{user.Balance:0.00}</b>";

        await _users.SetStateAsync(user.TelegramUserId, BotStates.Idle);
        await bot.SendTextMessageAsync(user.TelegramUserId, text, parseMode: ParseMode.Html, replyMarkup: BotKeyboards.ProfileMenu(lang));
    }

    private async Task ShowHelpAsync(ITelegramBotClient bot, AppUser user)
    {
        var lang = BotTexts.Normalize(user.Language);
        var tariffsUrl = await GetTariffsUrlAsync();
        var text = string.Format(BotTexts.Text(lang, BotTextKeys.HelpText), tariffsUrl);

        await bot.SendTextMessageAsync(user.TelegramUserId, text, replyMarkup: BotKeyboards.MainMenu(lang));
    }

    private async Task StartCreateFlowAsync(ITelegramBotClient bot, AppUser user)
    {
        var lang = BotTexts.Normalize(user.Language);
        if (string.IsNullOrWhiteSpace(user.Country) || string.IsNullOrWhiteSpace(user.City))
        {
            await _users.SetStateAsync(user.TelegramUserId, BotStates.AwaitCountry);
            await bot.SendTextMessageAsync(user.TelegramUserId, BotTexts.Text(lang, BotTextKeys.EnterCountryFirst));
            await SendCountryPromptAsync(bot, user, forceManualPrompt: false);
            return;
        }

        var roots = await _categories.GetRootAsync();
        await _users.SetStateAsync(user.TelegramUserId, BotStates.Creating_SelectCategory, new FlowContext { CategoryParentId = null });

        await bot.SendTextMessageAsync(user.TelegramUserId, BotTexts.Text(lang, BotTextKeys.CategoryChoose),
            replyMarkup: BotKeyboards.Categories(roots));
    }

    private async Task StartSearchFlowAsync(ITelegramBotClient bot, AppUser user)
    {
        var lang = BotTexts.Normalize(user.Language);
        var roots = await _categories.GetRootAsync();
        await _users.SetStateAsync(user.TelegramUserId, BotStates.Searching_SelectCategory, new FlowContext { CategoryParentId = null });

        await bot.SendTextMessageAsync(user.TelegramUserId, BotTexts.Text(lang, BotTextKeys.CategoryChooseSearch),
            replyMarkup: BotKeyboards.Categories(roots));
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

        if (data.StartsWith("lang:", StringComparison.Ordinal))
        {
            await HandleLanguageCallbackAsync(bot, cq, user, data);
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
        var lang = BotTexts.Normalize(user.Language);
        var idStr = data.Split(':', 2)[1];
        if (!Guid.TryParse(idStr, out var catId))
        {
            await bot.AnswerCallbackQueryAsync(cq.Id, BotTexts.Text(lang, BotTextKeys.InvalidCategory));
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
                text: BotTexts.Text(lang, BotTextKeys.SubcategoryChoose),
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
            var allowFree = await _settings.GetBoolAsync("Ads.EnableFreeAds", defaultValue: true);
            await bot.EditMessageTextAsync(
                chatId: cq.Message!.Chat.Id,
                messageId: cq.Message!.MessageId,
                text: BotTexts.Text(lang, BotTextKeys.AdTypeChoose),
                replyMarkup: BotKeyboards.AdType(lang, allowFree));
        }
        else if (state.State == BotStates.Searching_SelectCategory)
        {
            await _users.SetStateAsync(user.TelegramUserId, BotStates.Searching_AwaitKeywords, ctx2);
            await bot.SendTextMessageAsync(user.TelegramUserId, BotTexts.Text(lang, BotTextKeys.SearchKeywords));
        }

        await bot.AnswerCallbackQueryAsync(cq.Id);
    }

    private async Task HandleCategoryBackCallbackAsync(ITelegramBotClient bot, CallbackQuery cq, AppUser user, UserState state, string data)
    {
        var lang = BotTexts.Normalize(user.Language);
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
            text: BotTexts.Text(lang, BotTextKeys.CategoryChoose),
            replyMarkup: BotKeyboards.Categories(cats, back));

        await bot.AnswerCallbackQueryAsync(cq.Id);
    }

    private async Task HandleTypeCallbackAsync(ITelegramBotClient bot, CallbackQuery cq, AppUser user, UserState state, string data)
    {
        var lang = BotTexts.Normalize(user.Language);
        var ctx = await _users.GetStatePayloadAsync<FlowContext>(user.TelegramUserId) ?? new FlowContext();
        if (ctx.SelectedCategoryId == null)
        {
            await bot.AnswerCallbackQueryAsync(cq.Id, BotTexts.Text(lang, BotTextKeys.CategoryNotSelected));
            return;
        }

        var isPaid = data == "type:paid";
        var allowFree = await _settings.GetBoolAsync("Ads.EnableFreeAds", defaultValue: true);
        if (!isPaid && !allowFree)
        {
            await bot.AnswerCallbackQueryAsync(cq.Id, BotTexts.Text(lang, BotTextKeys.FreeAdsDisabled));
            return;
        }

        try
        {
            var draft = await _ads.CreateDraftAsync(user, ctx.SelectedCategoryId.Value, isPaid);
            ctx.DraftAdId = draft.Id;

            await _users.SetStateAsync(user.TelegramUserId, BotStates.Creating_AwaitTitle, ctx);

            var tariffsUrl = await GetTariffsUrlAsync();
            var text = isPaid
                ? string.Format(BotTexts.Text(lang, BotTextKeys.PaidAdPrefix), tariffsUrl)
                : BotTexts.Text(lang, BotTextKeys.EnterTitle);

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
        var lang = BotTexts.Normalize(user.Language);
        var ctx = await _users.GetStatePayloadAsync<FlowContext>(user.TelegramUserId) ?? new FlowContext();
        if (ctx.DraftAdId == null)
        {
            await _users.SetStateAsync(user.TelegramUserId, BotStates.Idle);
            await bot.SendTextMessageAsync(user.TelegramUserId, BotTexts.Text(lang, BotTextKeys.DraftNotFound),
                replyMarkup: BotKeyboards.MainMenu(lang));
            return;
        }

        var ad = await _ads.GetByIdAsync(ctx.DraftAdId.Value);
        if (ad == null)
        {
            await _users.SetStateAsync(user.TelegramUserId, BotStates.Idle);
            await bot.SendTextMessageAsync(user.TelegramUserId, BotTexts.Text(lang, BotTextKeys.DraftNotFound),
                replyMarkup: BotKeyboards.MainMenu(lang));
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

            if (!string.IsNullOrWhiteSpace(ctx.EditField))
            {
                ctx.EditField = null;
                await ShowPreviewAsync(bot, user, ad.Id);
                return;
            }

            await _users.SetStateAsync(user.TelegramUserId, BotStates.Creating_AwaitText, ctx);
            await bot.SendTextMessageAsync(user.TelegramUserId, BotTexts.Text(lang, BotTextKeys.EnterText));
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

            if (!string.IsNullOrWhiteSpace(ctx.EditField))
            {
                ctx.EditField = null;
                await ShowPreviewAsync(bot, user, ad.Id);
                return;
            }

            await _users.SetStateAsync(user.TelegramUserId, BotStates.Creating_AwaitContacts, ctx);
            await bot.SendTextMessageAsync(user.TelegramUserId, BotTexts.Text(lang, BotTextKeys.EnterContacts));
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

            if (!string.IsNullOrWhiteSpace(ctx.EditField))
            {
                ctx.EditField = null;
                await ShowPreviewAsync(bot, user, ad.Id);
                return;
            }

            var allowMediaInFree = await _settings.GetBoolAsync("Ads.FreeAllowMedia", defaultValue: false);
            if (ad.IsPaid || allowMediaInFree)
            {
                await _users.SetStateAsync(user.TelegramUserId, BotStates.Creating_AwaitMedia, ctx);
                await bot.SendTextMessageAsync(user.TelegramUserId, BotTexts.Text(lang, BotTextKeys.SendMedia),
                    replyMarkup: BotKeyboards.SkipMedia(lang));
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
            if (BotTexts.Matches(message.Text, lang, BotTextKeys.Skip))
            {
                await ShowPreviewAsync(bot, user, ad.Id);
                return;
            }

            if (message.Photo != null && message.Photo.Length > 0)
            {
                var fileId = message.Photo[^1].FileId;
                var (ok, err) = await _ads.SetMediaAsync(ad.Id, MediaType.Photo, fileId);
                if (!ok)
                {
                    await bot.SendTextMessageAsync(user.TelegramUserId, err ?? "Ошибка.");
                    return;
                }
                await ShowPreviewAsync(bot, user, ad.Id);
                return;
            }

            if (message.Video != null)
            {
                var (ok, err) = await _ads.SetMediaAsync(ad.Id, MediaType.Video, message.Video.FileId);
                if (!ok)
                {
                    await bot.SendTextMessageAsync(user.TelegramUserId, err ?? "Ошибка.");
                    return;
                }
                await ShowPreviewAsync(bot, user, ad.Id);
                return;
            }

            await bot.SendTextMessageAsync(user.TelegramUserId, BotTexts.Text(lang, BotTextKeys.SendMediaRetry));
        }
    }

    private async Task ShowPreviewAsync(ITelegramBotClient bot, AppUser user, Guid adId)
    {
        var lang = BotTexts.Normalize(user.Language);
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

        var allowMediaInFree = await _settings.GetBoolAsync("Ads.FreeAllowMedia", defaultValue: false);
        var kb = ad.IsPaid
            ? BotKeyboards.PreviewPaid(adId, lang)
            : BotKeyboards.PreviewFree(adId, allowMediaInFree, lang);

        if (ad.MediaType != MediaType.None && !string.IsNullOrWhiteSpace(ad.MediaFileId))
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
        var lang = BotTexts.Normalize(user.Language);
        var ads = await _ads.GetUserAdsAsync(user.Id, take: 10);
        if (ads.Count == 0)
        {
            await bot.SendTextMessageAsync(user.TelegramUserId, BotTexts.Text(lang, BotTextKeys.NoAdsYet),
                replyMarkup: BotKeyboards.MainMenu(lang));
            return;
        }

        await bot.SendTextMessageAsync(user.TelegramUserId, BotTexts.Text(lang, BotTextKeys.MyAdsHeader));

        foreach (var ad in ads)
        {
            var category = await _categories.GetByIdAsync(ad.CategoryId);
            var title = string.IsNullOrWhiteSpace(ad.Title) ? BotTexts.Text(lang, BotTextKeys.AdNoTitle) : ad.Title;
            var statusText = GetAdStatusLabel(ad.Status, lang);
            var typeText = ad.IsPaid ? BotTexts.Text(lang, BotTextKeys.AdTypePaid) : BotTexts.Text(lang, BotTextKeys.AdTypeFree);

            var text = $"<b>{Escape(title)}</b>\n" +
                       $"Статус: <b>{statusText}</b>\n" +
                       $"Тип: {typeText}\n" +
                       $"Категория: {Escape(category?.Name ?? "—")}\n" +
                       $"Создано: {ad.CreatedAtUtc:yyyy-MM-dd HH:mm} UTC";

            var rows = new List<List<InlineKeyboardButton>>
            {
                new()
                {
                    InlineKeyboardButton.WithCallbackData(BotTexts.Text(lang, BotTextKeys.AdDetails), $"myad:view:{ad.Id}")
                }
            };

            if (ad.Status == AdStatus.Published)
            {
                var publishedRow = new List<InlineKeyboardButton>
                {
                    InlineKeyboardButton.WithCallbackData(BotTexts.Text(lang, BotTextKeys.AdLinks), $"myad:links:{ad.Id}")
                };

                if (ad.IsPaid)
                    publishedRow.Add(InlineKeyboardButton.WithCallbackData(BotTexts.Text(lang, BotTextKeys.AdBump), $"myad:bump:{ad.Id}"));

                rows.Add(publishedRow);
            }

            await bot.SendTextMessageAsync(user.TelegramUserId, text, parseMode: ParseMode.Html, replyMarkup: new InlineKeyboardMarkup(rows));
        }
    }

    private async Task HandleMyAdsCallbackAsync(ITelegramBotClient bot, CallbackQuery cq, AppUser user, string data)
    {
        var lang = BotTexts.Normalize(user.Language);
        var parts = data.Split(':', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 3)
        {
            await bot.AnswerCallbackQueryAsync(cq.Id);
            return;
        }

        var action = parts[1];
        if (!Guid.TryParse(parts[2], out var adId))
        {
            await bot.AnswerCallbackQueryAsync(cq.Id, BotTexts.Text(lang, BotTextKeys.InvalidId));
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
            var statusText = GetAdStatusLabel(ad.Status, lang);
            var typeText = ad.IsPaid ? BotTexts.Text(lang, BotTextKeys.AdTypePaid) : BotTexts.Text(lang, BotTextKeys.AdTypeFree);

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
                await bot.AnswerCallbackQueryAsync(cq.Id, BotTexts.Text(lang, BotTextKeys.LinksOnlyPublished));
                return;
            }

            var pubs = await _db.AdPublications
                .Where(x => x.AdId == adId)
                .OrderByDescending(x => x.PublishedAtUtc)
                .ToListAsync();

            if (pubs.Count == 0)
            {
                await bot.AnswerCallbackQueryAsync(cq.Id, BotTexts.Text(lang, BotTextKeys.AdLinksUnavailable));
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
                await bot.SendTextMessageAsync(user.TelegramUserId, BotTexts.Text(lang, BotTextKeys.AdLinksUnavailableNoUsername));
            }
            else
            {
                await bot.SendTextMessageAsync(user.TelegramUserId,
                    string.Format(BotTexts.Text(lang, BotTextKeys.AdLinksHeader), string.Join("\n", links)));
            }

            await bot.AnswerCallbackQueryAsync(cq.Id);
            return;
        }

        if (action == "bump")
        {
            if (!ad.IsPaid)
            {
                await bot.AnswerCallbackQueryAsync(cq.Id, BotTexts.Text(lang, BotTextKeys.BumpPaidOnly));
                return;
            }

            if (ad.Status != AdStatus.Published)
            {
                await bot.AnswerCallbackQueryAsync(cq.Id, BotTexts.Text(lang, BotTextKeys.BumpPublishedOnly));
                return;
            }

            await _payments.SendBumpInvoiceAsync(user.TelegramUserId, ad.Id);
            await bot.SendTextMessageAsync(user.TelegramUserId, BotTexts.Text(lang, BotTextKeys.BumpInvoiceSent));
            await bot.AnswerCallbackQueryAsync(cq.Id);
            return;
        }

        await bot.AnswerCallbackQueryAsync(cq.Id);
    }

    private async Task SendReferralLinkAsync(ITelegramBotClient bot, AppUser user)
    {
        var lang = BotTexts.Normalize(user.Language);
        var enabled = await _settings.GetBoolAsync("App.EnableReferralProgram", _appOptions.EnableReferralProgram);
        if (!enabled)
        {
            await bot.SendTextMessageAsync(user.TelegramUserId, BotTexts.Text(lang, BotTextKeys.ReferralDisabled));
            return;
        }

        var me = await bot.GetMeAsync();
        if (string.IsNullOrWhiteSpace(me.Username))
        {
            await bot.SendTextMessageAsync(user.TelegramUserId, BotTexts.Text(lang, BotTextKeys.BotUsernameMissing));
            return;
        }

        var refLink = $"https://t.me/{me.Username}?start=ref_{user.ReferralCode}";
        var text = string.Format(BotTexts.Text(lang, BotTextKeys.ReferralLinkText), refLink);

        await bot.SendTextMessageAsync(user.TelegramUserId, text);
    }

    private async Task HandleCreateCallbackAsync(ITelegramBotClient bot, CallbackQuery cq, AppUser user, string data)
    {
        var lang = BotTexts.Normalize(user.Language);
        var parts = data.Split(':', StringSplitOptions.RemoveEmptyEntries);

        // create:cancel
        if (parts.Length == 2 && parts[1] == "cancel")
        {
            await _users.SetStateAsync(user.TelegramUserId, BotStates.Idle, payload: null);
            await bot.SendTextMessageAsync(user.TelegramUserId, BotTexts.Text(lang, BotTextKeys.PublishCanceled),
                replyMarkup: BotKeyboards.MainMenu(lang));
            await bot.AnswerCallbackQueryAsync(cq.Id);
            return;
        }

        // create:edit:<field>:<adId>
        if (parts.Length == 4 && parts[1] == "edit")
        {
            var field = parts[2];
            if (!Guid.TryParse(parts[3], out var adId))
            {
                await bot.AnswerCallbackQueryAsync(cq.Id, BotTexts.Text(lang, BotTextKeys.InvalidId));
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
                    await bot.SendTextMessageAsync(user.TelegramUserId, "Отправьте новое фото/видео (или «Пропустить»).",
                        replyMarkup: BotKeyboards.SkipMedia(lang));
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
                await bot.AnswerCallbackQueryAsync(cq.Id, BotTexts.Text(lang, BotTextKeys.InvalidId));
                return;
            }

            var pub = await _publication.SubmitAsync(user.TelegramUserId, adId);

            if (pub.Ok)
            {
                var text = pub.Message;
                if (pub.PublishedLinks.Count > 0)
                    text += "\n\n" + string.Format(BotTexts.Text(lang, BotTextKeys.PublishLinksHeader),
                        string.Join("\n", pub.PublishedLinks));

                await bot.SendTextMessageAsync(user.TelegramUserId, text, replyMarkup: BotKeyboards.MainMenu(lang));
            }
            else
            {
                await bot.SendTextMessageAsync(user.TelegramUserId, pub.Message, replyMarkup: BotKeyboards.MainMenu(lang));
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
                await bot.AnswerCallbackQueryAsync(cq.Id, BotTexts.Text(lang, BotTextKeys.InvalidId));
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
        var lang = BotTexts.Normalize(user.Language);
        var parts = data.Split(':', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 3)
        {
            await bot.AnswerCallbackQueryAsync(cq.Id);
            return;
        }

        var action = parts[1];
        if (!Guid.TryParse(parts[2], out var adId))
        {
            await bot.AnswerCallbackQueryAsync(cq.Id, BotTexts.Text(lang, BotTextKeys.InvalidId));
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

            await bot.SendTextMessageAsync(user.TelegramUserId,
                string.Format(BotTexts.Text(lang, BotTextKeys.AdContactsAuthor), ad.Contacts));
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
                await bot.AnswerCallbackQueryAsync(cq.Id, BotTexts.Text(lang, BotTextKeys.AdLinksUnavailable));
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
                await bot.SendTextMessageAsync(user.TelegramUserId, BotTexts.Text(lang, BotTextKeys.AdLinksUnavailableNoUsername));
            }
            else
            {
                await bot.SendTextMessageAsync(user.TelegramUserId,
                    string.Format(BotTexts.Text(lang, BotTextKeys.AdLinksHeader), string.Join("\n", links)));
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

    private async Task HandleLanguageCallbackAsync(ITelegramBotClient bot, CallbackQuery cq, AppUser user, string data)
    {
        var langCode = data.Split(':', 2).LastOrDefault()?.Trim().ToLowerInvariant();
        if (langCode is not ("ru" or "en"))
        {
            await bot.AnswerCallbackQueryAsync(cq.Id);
            return;
        }

        await _users.UpdateLanguageAsync(user.TelegramUserId, langCode);
        var lang = BotTexts.Normalize(langCode);

        await _users.SetStateAsync(user.TelegramUserId, BotStates.Idle);
        await bot.SendTextMessageAsync(user.TelegramUserId, BotTexts.Text(lang, BotTextKeys.LanguageSet));
        await bot.SendTextMessageAsync(user.TelegramUserId, BotTexts.Text(lang, BotTextKeys.StartGreeting),
            replyMarkup: BotKeyboards.MainMenu(lang));
        await bot.AnswerCallbackQueryAsync(cq.Id);
    }

    private enum LocationInputMode
    {
        Manual,
        Keyboard
    }

    private async Task<(LocationInputMode mode, List<string> countries)> GetLocationModeAsync()
    {
        var modeRaw = (await _settings.GetAsync("Location.InputMode"))?.Trim();
        var mode = string.Equals(modeRaw, "keyboard", StringComparison.OrdinalIgnoreCase)
            ? LocationInputMode.Keyboard
            : LocationInputMode.Manual;

        var countriesRaw = (await _settings.GetAsync("Location.Countries"))?.Trim();
        var countries = ParseStringList(countriesRaw);

        if (mode == LocationInputMode.Keyboard && countries.Count == 0)
            mode = LocationInputMode.Manual;

        return (mode, countries);
    }

    private async Task<(LocationInputMode mode, List<string> cities)> GetCitiesAsync(string country)
    {
        var modeRaw = (await _settings.GetAsync("Location.InputMode"))?.Trim();
        var mode = string.Equals(modeRaw, "keyboard", StringComparison.OrdinalIgnoreCase)
            ? LocationInputMode.Keyboard
            : LocationInputMode.Manual;

        var citiesMapRaw = (await _settings.GetAsync("Location.CitiesMap"))?.Trim();
        if (string.IsNullOrWhiteSpace(citiesMapRaw))
            return (LocationInputMode.Manual, new List<string>());

        try
        {
            var map = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(citiesMapRaw)
                      ?? new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            if (map.TryGetValue(country, out var list) && list != null)
            {
                if (mode == LocationInputMode.Keyboard && list.Count == 0)
                    return (LocationInputMode.Manual, new List<string>());

                return (mode, list);
            }
        }
        catch (JsonException)
        {
            // ignore invalid settings
        }

        return (LocationInputMode.Manual, new List<string>());
    }

    private static List<string> ParseStringList(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return new List<string>();

        try
        {
            var list = JsonSerializer.Deserialize<List<string>>(raw);
            if (list != null && list.Count > 0)
                return list;
        }
        catch (JsonException)
        {
            // fallback to csv
        }

        return raw.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();
    }

    private async Task SendCountryPromptAsync(ITelegramBotClient bot, AppUser user, bool forceManualPrompt = true)
    {
        var lang = BotTexts.Normalize(user.Language);
        var (mode, countries) = await GetLocationModeAsync();

        if (mode == LocationInputMode.Keyboard && countries.Count > 0)
        {
            await bot.SendTextMessageAsync(user.TelegramUserId, BotTexts.Text(lang, BotTextKeys.SelectCountry),
                replyMarkup: BotKeyboards.LocationOptions(countries, lang));
            return;
        }

        if (forceManualPrompt)
            await bot.SendTextMessageAsync(user.TelegramUserId, BotTexts.Text(lang, BotTextKeys.EnterCountry));
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

    private static string GetAdStatusLabel(AdStatus status, string language) => status switch
    {
        AdStatus.Draft => BotTexts.Text(language, BotTextKeys.AdStatusDraft),
        AdStatus.PendingModeration => BotTexts.Text(language, BotTextKeys.AdStatusPending),
        AdStatus.Published => BotTexts.Text(language, BotTextKeys.AdStatusPublished),
        AdStatus.Rejected => BotTexts.Text(language, BotTextKeys.AdStatusRejected),
        _ => status.ToString()
    };

    private static string Escape(string? s)
        => System.Net.WebUtility.HtmlEncode(s ?? string.Empty);
}
