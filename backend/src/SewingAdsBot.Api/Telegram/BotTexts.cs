using System.Collections.Immutable;

namespace SewingAdsBot.Api.Telegram;

/// <summary>
/// Локализованные тексты бота (RU/EN).
/// </summary>
public static class BotTexts
{
    public const string Ru = "ru";
    public const string En = "en";

    private static readonly ImmutableDictionary<string, ImmutableDictionary<string, string>> Texts
        = new Dictionary<string, ImmutableDictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
        {
            [Ru] = BuildRu(),
            [En] = BuildEn()
        }.ToImmutableDictionary(StringComparer.OrdinalIgnoreCase);

    public static string Normalize(string? language)
    {
        if (string.IsNullOrWhiteSpace(language))
            return Ru;

        return language.Trim().ToLowerInvariant() switch
        {
            "ru" or "rus" or "russian" => Ru,
            "en" or "eng" or "english" => En,
            _ => Ru
        };
    }

    public static string Text(string language, string key)
    {
        var lang = Normalize(language);
        if (Texts.TryGetValue(lang, out var dict) && dict.TryGetValue(key, out var value))
            return value;

        if (Texts[Ru].TryGetValue(key, out var fallback))
            return fallback;

        return key;
    }

    public static bool Matches(string? input, string language, string key)
        => string.Equals(input?.Trim(), Text(language, key), StringComparison.OrdinalIgnoreCase);

    private static ImmutableDictionary<string, string> BuildRu()
        => new Dictionary<string, string>
        {
            [BotTextKeys.LanguageChooseTitle] = "Выберите язык / Choose language:",
            [BotTextKeys.LanguageRu] = "Русский",
            [BotTextKeys.LanguageEn] = "English",
            [BotTextKeys.Cancel] = "Отмена",
            [BotTextKeys.Canceled] = "Ок, отменено.",
            [BotTextKeys.NotUnderstood] = "Не понял. Откройте меню.",
            [BotTextKeys.MainMenuTitle] = "Главное меню:",
            [BotTextKeys.MenuCreateAd] = "Создать объявление",
            [BotTextKeys.MenuSearchAd] = "Найти объявление",
            [BotTextKeys.MenuProfile] = "Мой профиль",
            [BotTextKeys.MenuHelp] = "Помощь",
            [BotTextKeys.MenuPaidAd] = "Платное объявление",
            [BotTextKeys.MenuLocation] = "Место",
            [BotTextKeys.MenuMyAds] = "Мои объявления",
            [BotTextKeys.MenuReferral] = "Реферальная ссылка",
            [BotTextKeys.MenuBack] = "Назад",
            [BotTextKeys.PaidTariffs] = "Тарифы",
            [BotTextKeys.PaidAdInfo] = "Чтобы разместить платное объявление, нажмите «Создать объявление» и выберите «Платное».",
            [BotTextKeys.PaidAdInfoTitle] = "Тарифы на платные размещения:",
            [BotTextKeys.ProfileTitle] = "Профиль",
            [BotTextKeys.LocationNotSet] = "не задано",
            [BotTextKeys.ProfileLocation] = "Место",
            [BotTextKeys.ProfileReferral] = "Реф.код",
            [BotTextKeys.ProfileBalance] = "Баланс",
            [BotTextKeys.HelpText] = "Правила:\n" +
                                     "• Бесплатные объявления: 1 раз в сутки, без фото/видео и без ссылок.\n" +
                                     "• Контакты: только @username, телефон или email.\n" +
                                     "• Платные объявления: можно фото/видео и ссылки.\n\n" +
                                     "Тарифы: {0}",
            [BotTextKeys.StartGreeting] = "Привет! Это бот объявлений для швейной индустрии.\n\n" +
                                          "• Бесплатно: без фото/видео и без ссылок, 1 раз в сутки.\n" +
                                          "• Платно: можно фото/видео и ссылки, плюс платное поднятие.\n\n" +
                                          "Выберите действие в меню ниже.",
            [BotTextKeys.EnterCountry] = "Введите страну:",
            [BotTextKeys.EnterCountryShort] = "Введите страну (минимум 2 символа).",
            [BotTextKeys.EnterCountryFirst] = "Сначала укажите страну. (Профиль → Место)\nВведите страну:",
            [BotTextKeys.SelectCountry] = "Выберите страну:",
            [BotTextKeys.EnterCity] = "Теперь введите город.",
            [BotTextKeys.EnterCityShort] = "Введите город (минимум 2 символа).",
            [BotTextKeys.SelectCity] = "Выберите город:",
            [BotTextKeys.LocationSaved] = "Место сохранено ✅",
            [BotTextKeys.CategoryChoose] = "Выберите категорию:",
            [BotTextKeys.SubcategoryChoose] = "Выберите подкатегорию:",
            [BotTextKeys.CategoryChooseSearch] = "Выберите категорию для поиска:",
            [BotTextKeys.CategoryNotSelected] = "Категория не выбрана.",
            [BotTextKeys.SearchKeywords] = "Введите ключевые слова для поиска (или отправьте «-» чтобы искать без слов):",
            [BotTextKeys.SearchResultsCount] = "Найдено объявлений: {0}",
            [BotTextKeys.SearchNothingFound] = "Ничего не найдено.",
            [BotTextKeys.SearchGoToAd] = "Перейти к объявлению",
            [BotTextKeys.SearchViewContact] = "Посмотреть контакт",
            [BotTextKeys.ChooseActionMenu] = "Выберите действие из меню.",
            [BotTextKeys.AdTypeChoose] = "Выберите тип объявления:",
            [BotTextKeys.AdTypeFree] = "Бесплатное",
            [BotTextKeys.AdTypePaid] = "Платное",
            [BotTextKeys.PaidAdPrefix] = "Платное объявление ✅\nТарифы: {0}\n\nВведите заголовок объявления:",
            [BotTextKeys.EnterTitle] = "Введите заголовок объявления:",
            [BotTextKeys.EnterText] = "Введите текст объявления:",
            [BotTextKeys.EnterContacts] = "Введите контакты (только @username, телефон или email):",
            [BotTextKeys.SendMedia] = "Отправьте фото или видео (или нажмите «Пропустить»).",
            [BotTextKeys.SendMediaRetry] = "Отправьте фото/видео или нажмите «Пропустить».",
            [BotTextKeys.Skip] = "Пропустить",
            [BotTextKeys.DraftNotFound] = "Черновик не найден. Начните заново.",
            [BotTextKeys.NoAdsYet] = "У вас пока нет объявлений.",
            [BotTextKeys.MyAdsHeader] = "Ваши объявления (последние 10):",
            [BotTextKeys.AdNoTitle] = "Без заголовка",
            [BotTextKeys.AdStatusDraft] = "Черновик",
            [BotTextKeys.AdStatusPending] = "На модерации",
            [BotTextKeys.AdStatusPublished] = "Опубликовано",
            [BotTextKeys.AdStatusRejected] = "Отклонено",
            [BotTextKeys.AdDetails] = "Подробнее",
            [BotTextKeys.AdLinks] = "Ссылки",
            [BotTextKeys.AdBump] = "Поднять",
            [BotTextKeys.AdContactsAuthor] = "Контакты автора:\n{0}",
            [BotTextKeys.AdLinksUnavailable] = "Ссылки недоступны.",
            [BotTextKeys.AdLinksUnavailableNoUsername] = "Ссылки не удалось сформировать (каналы без username).",
            [BotTextKeys.AdLinksHeader] = "Ссылки:\n{0}",
            [BotTextKeys.BumpPaidOnly] = "Поднятие доступно только для платных объявлений.",
            [BotTextKeys.BumpPublishedOnly] = "Поднять можно только опубликованные объявления.",
            [BotTextKeys.BumpInvoiceSent] = "Счёт на поднятие отправлен ✅",
            [BotTextKeys.ReferralDisabled] = "Реферальная программа сейчас отключена.",
            [BotTextKeys.BotUsernameMissing] = "Не удалось определить username бота.",
            [BotTextKeys.ReferralLinkText] = "Ваша реферальная ссылка:\n{0}\n\nПоделитесь ею, чтобы получать бонусы за оплаты привлечённых пользователей.",
            [BotTextKeys.PublishLinksHeader] = "Ссылки:\n{0}",
            [BotTextKeys.PublishOk] = "Опубликовано.",
            [BotTextKeys.PublishCanceled] = "Ок, отменено.",
            [BotTextKeys.InvalidCategory] = "Некорректная категория.",
            [BotTextKeys.InvalidId] = "Некорректный ID.",
            [BotTextKeys.LinksOnlyPublished] = "Ссылки доступны только для опубликованных объявлений.",
            [BotTextKeys.InvalidLocationSelection] = "Выберите значение из списка или нажмите «Отмена».",
            [BotTextKeys.LanguageSet] = "Язык сохранён ✅",
            [BotTextKeys.LocationInputManualHint] = "Введите страну вручную.",
            [BotTextKeys.LocationInputManualCityHint] = "Введите город вручную.",
            [BotTextKeys.LocationInputInvalidMode] = "Настройки местоположения не заданы, пожалуйста введите вручную.",
            [BotTextKeys.FreeAdsDisabled] = "Бесплатные объявления отключены администратором.",
            [BotTextKeys.PreviewPublish] = "✅ Опубликовать",
            [BotTextKeys.PreviewPayPublish] = "💳 Оплатить и опубликовать",
            [BotTextKeys.PreviewEditTitle] = "✏️ Заголовок",
            [BotTextKeys.PreviewEditText] = "✏️ Текст",
            [BotTextKeys.PreviewEditContacts] = "✏️ Контакты",
            [BotTextKeys.PreviewEditMedia] = "🖼 Медиа",
            [BotTextKeys.PreviewCancel] = "❌ Отменить"
        }.ToImmutableDictionary();

    private static ImmutableDictionary<string, string> BuildEn()
        => new Dictionary<string, string>
        {
            [BotTextKeys.LanguageChooseTitle] = "Choose language / Выберите язык:",
            [BotTextKeys.LanguageRu] = "Русский",
            [BotTextKeys.LanguageEn] = "English",
            [BotTextKeys.Cancel] = "Cancel",
            [BotTextKeys.Canceled] = "Okay, canceled.",
            [BotTextKeys.NotUnderstood] = "I didn't understand. Open the menu.",
            [BotTextKeys.MainMenuTitle] = "Main menu:",
            [BotTextKeys.MenuCreateAd] = "Create ad",
            [BotTextKeys.MenuSearchAd] = "Find ad",
            [BotTextKeys.MenuProfile] = "My profile",
            [BotTextKeys.MenuHelp] = "Help",
            [BotTextKeys.MenuPaidAd] = "Paid ad",
            [BotTextKeys.MenuLocation] = "Location",
            [BotTextKeys.MenuMyAds] = "My ads",
            [BotTextKeys.MenuReferral] = "Referral link",
            [BotTextKeys.MenuBack] = "Back",
            [BotTextKeys.PaidTariffs] = "Tariffs",
            [BotTextKeys.PaidAdInfo] = "To place a paid ad, press “Create ad” and choose “Paid”.",
            [BotTextKeys.PaidAdInfoTitle] = "Paid placement tariffs:",
            [BotTextKeys.ProfileTitle] = "Profile",
            [BotTextKeys.LocationNotSet] = "not set",
            [BotTextKeys.ProfileLocation] = "Location",
            [BotTextKeys.ProfileReferral] = "Ref.code",
            [BotTextKeys.ProfileBalance] = "Balance",
            [BotTextKeys.HelpText] = "Rules:\n" +
                                     "• Free ads: 1 per day, no photo/video and no links.\n" +
                                     "• Contacts: only @username, phone, or email.\n" +
                                     "• Paid ads: photo/video and links allowed.\n\n" +
                                     "Tariffs: {0}",
            [BotTextKeys.StartGreeting] = "Hi! This is a classifieds bot for the sewing industry.\n\n" +
                                          "• Free: no photo/video and no links, once per day.\n" +
                                          "• Paid: photo/video and links allowed, plus paid bumps.\n\n" +
                                          "Choose an action from the menu below.",
            [BotTextKeys.EnterCountry] = "Enter your country:",
            [BotTextKeys.EnterCountryShort] = "Enter the country (at least 2 characters).",
            [BotTextKeys.EnterCountryFirst] = "Please set your country first. (Profile → Location)\nEnter your country:",
            [BotTextKeys.SelectCountry] = "Choose your country:",
            [BotTextKeys.EnterCity] = "Now enter your city.",
            [BotTextKeys.EnterCityShort] = "Enter the city (at least 2 characters).",
            [BotTextKeys.SelectCity] = "Choose your city:",
            [BotTextKeys.LocationSaved] = "Location saved ✅",
            [BotTextKeys.CategoryChoose] = "Choose a category:",
            [BotTextKeys.SubcategoryChoose] = "Choose a subcategory:",
            [BotTextKeys.CategoryChooseSearch] = "Choose a category to search:",
            [BotTextKeys.CategoryNotSelected] = "Category not selected.",
            [BotTextKeys.SearchKeywords] = "Enter keywords to search (or send “-” to search without keywords):",
            [BotTextKeys.SearchResultsCount] = "Found ads: {0}",
            [BotTextKeys.SearchNothingFound] = "Nothing found.",
            [BotTextKeys.SearchGoToAd] = "Open ad",
            [BotTextKeys.SearchViewContact] = "View contact",
            [BotTextKeys.ChooseActionMenu] = "Choose an action from the menu.",
            [BotTextKeys.AdTypeChoose] = "Choose ad type:",
            [BotTextKeys.AdTypeFree] = "Free",
            [BotTextKeys.AdTypePaid] = "Paid",
            [BotTextKeys.PaidAdPrefix] = "Paid ad ✅\nTariffs: {0}\n\nEnter the ad title:",
            [BotTextKeys.EnterTitle] = "Enter the ad title:",
            [BotTextKeys.EnterText] = "Enter the ad text:",
            [BotTextKeys.EnterContacts] = "Enter contacts (only @username, phone, or email):",
            [BotTextKeys.SendMedia] = "Send a photo or video (or press “Skip”).",
            [BotTextKeys.SendMediaRetry] = "Send photo/video or press “Skip”.",
            [BotTextKeys.Skip] = "Skip",
            [BotTextKeys.DraftNotFound] = "Draft not found. Start over.",
            [BotTextKeys.NoAdsYet] = "You have no ads yet.",
            [BotTextKeys.MyAdsHeader] = "Your ads (latest 10):",
            [BotTextKeys.AdNoTitle] = "Untitled",
            [BotTextKeys.AdStatusDraft] = "Draft",
            [BotTextKeys.AdStatusPending] = "On moderation",
            [BotTextKeys.AdStatusPublished] = "Published",
            [BotTextKeys.AdStatusRejected] = "Rejected",
            [BotTextKeys.AdDetails] = "Details",
            [BotTextKeys.AdLinks] = "Links",
            [BotTextKeys.AdBump] = "Bump",
            [BotTextKeys.AdContactsAuthor] = "Author contacts:\n{0}",
            [BotTextKeys.AdLinksUnavailable] = "Links are unavailable.",
            [BotTextKeys.AdLinksUnavailableNoUsername] = "Couldn't build links (channels without username).",
            [BotTextKeys.AdLinksHeader] = "Links:\n{0}",
            [BotTextKeys.BumpPaidOnly] = "Bumps are available only for paid ads.",
            [BotTextKeys.BumpPublishedOnly] = "You can bump only published ads.",
            [BotTextKeys.BumpInvoiceSent] = "Bump invoice sent ✅",
            [BotTextKeys.ReferralDisabled] = "The referral program is currently disabled.",
            [BotTextKeys.BotUsernameMissing] = "Couldn't determine bot username.",
            [BotTextKeys.ReferralLinkText] = "Your referral link:\n{0}\n\nShare it to earn bonuses from payments of referred users.",
            [BotTextKeys.PublishLinksHeader] = "Links:\n{0}",
            [BotTextKeys.PublishOk] = "Published.",
            [BotTextKeys.PublishCanceled] = "Okay, canceled.",
            [BotTextKeys.InvalidCategory] = "Invalid category.",
            [BotTextKeys.InvalidId] = "Invalid ID.",
            [BotTextKeys.LinksOnlyPublished] = "Links are available only for published ads.",
            [BotTextKeys.InvalidLocationSelection] = "Select an item from the list or press “Cancel”.",
            [BotTextKeys.LanguageSet] = "Language saved ✅",
            [BotTextKeys.LocationInputManualHint] = "Please enter the country manually.",
            [BotTextKeys.LocationInputManualCityHint] = "Please enter the city manually.",
            [BotTextKeys.LocationInputInvalidMode] = "Location settings are missing, please enter manually.",
            [BotTextKeys.FreeAdsDisabled] = "Free ads are disabled by the administrator.",
            [BotTextKeys.PreviewPublish] = "✅ Publish",
            [BotTextKeys.PreviewPayPublish] = "💳 Pay & publish",
            [BotTextKeys.PreviewEditTitle] = "✏️ Title",
            [BotTextKeys.PreviewEditText] = "✏️ Text",
            [BotTextKeys.PreviewEditContacts] = "✏️ Contacts",
            [BotTextKeys.PreviewEditMedia] = "🖼 Media",
            [BotTextKeys.PreviewCancel] = "❌ Cancel"
        }.ToImmutableDictionary();
}

/// <summary>
/// Ключи локализованных строк.
/// </summary>
public static class BotTextKeys
{
    public const string LanguageChooseTitle = "language.choose.title";
    public const string LanguageRu = "language.choice.ru";
    public const string LanguageEn = "language.choice.en";
    public const string LanguageSet = "language.set";
    public const string Cancel = "action.cancel";
    public const string Canceled = "action.canceled";
    public const string NotUnderstood = "message.not_understood";
    public const string MainMenuTitle = "menu.main.title";
    public const string MenuCreateAd = "menu.main.create";
    public const string MenuSearchAd = "menu.main.search";
    public const string MenuProfile = "menu.main.profile";
    public const string MenuHelp = "menu.main.help";
    public const string MenuPaidAd = "menu.main.paid";
    public const string MenuLocation = "menu.profile.location";
    public const string MenuMyAds = "menu.profile.myads";
    public const string MenuReferral = "menu.profile.referral";
    public const string MenuBack = "menu.profile.back";
    public const string PaidTariffs = "paid.tariffs.button";
    public const string PaidAdInfoTitle = "paid.info.title";
    public const string PaidAdInfo = "paid.info.text";
    public const string ProfileTitle = "profile.title";
    public const string ProfileLocation = "profile.location.label";
    public const string ProfileReferral = "profile.referral.label";
    public const string ProfileBalance = "profile.balance.label";
    public const string LocationNotSet = "profile.location.not_set";
    public const string HelpText = "help.text";
    public const string StartGreeting = "start.greeting";
    public const string EnterCountry = "location.enter.country";
    public const string EnterCountryShort = "location.enter.country.short";
    public const string EnterCountryFirst = "location.enter.country.first";
    public const string SelectCountry = "location.select.country";
    public const string EnterCity = "location.enter.city";
    public const string EnterCityShort = "location.enter.city.short";
    public const string SelectCity = "location.select.city";
    public const string LocationSaved = "location.saved";
    public const string LocationInputManualHint = "location.manual.country";
    public const string LocationInputManualCityHint = "location.manual.city";
    public const string LocationInputInvalidMode = "location.invalid.mode";
    public const string CategoryChoose = "category.choose";
    public const string SubcategoryChoose = "category.choose.sub";
    public const string CategoryChooseSearch = "category.choose.search";
    public const string CategoryNotSelected = "category.not_selected";
    public const string SearchKeywords = "search.keywords";
    public const string SearchResultsCount = "search.count";
    public const string SearchNothingFound = "search.nothing";
    public const string SearchGoToAd = "search.goto";
    public const string SearchViewContact = "search.contact";
    public const string ChooseActionMenu = "menu.choose_action";
    public const string AdTypeChoose = "ad.type.choose";
    public const string AdTypeFree = "ad.type.free";
    public const string AdTypePaid = "ad.type.paid";
    public const string PaidAdPrefix = "ad.paid.prefix";
    public const string EnterTitle = "ad.enter.title";
    public const string EnterText = "ad.enter.text";
    public const string EnterContacts = "ad.enter.contacts";
    public const string SendMedia = "ad.send.media";
    public const string SendMediaRetry = "ad.send.media.retry";
    public const string Skip = "ad.skip";
    public const string DraftNotFound = "ad.draft.not_found";
    public const string NoAdsYet = "ad.list.empty";
    public const string MyAdsHeader = "ad.list.header";
    public const string AdNoTitle = "ad.no_title";
    public const string AdStatusDraft = "ad.status.draft";
    public const string AdStatusPending = "ad.status.pending";
    public const string AdStatusPublished = "ad.status.published";
    public const string AdStatusRejected = "ad.status.rejected";
    public const string AdDetails = "ad.details";
    public const string AdLinks = "ad.links";
    public const string AdBump = "ad.bump";
    public const string AdContactsAuthor = "ad.contacts.author";
    public const string AdLinksUnavailable = "ad.links.unavailable";
    public const string AdLinksUnavailableNoUsername = "ad.links.unavailable.username";
    public const string AdLinksHeader = "ad.links.header";
    public const string BumpPaidOnly = "ad.bump.paid_only";
    public const string BumpPublishedOnly = "ad.bump.published_only";
    public const string BumpInvoiceSent = "ad.bump.invoice_sent";
    public const string ReferralDisabled = "referral.disabled";
    public const string BotUsernameMissing = "bot.username.missing";
    public const string ReferralLinkText = "referral.link.text";
    public const string PublishLinksHeader = "publish.links.header";
    public const string PublishOk = "publish.ok";
    public const string PublishCanceled = "publish.canceled";
    public const string InvalidCategory = "error.invalid_category";
    public const string InvalidId = "error.invalid_id";
    public const string LinksOnlyPublished = "error.links.only_published";
    public const string InvalidLocationSelection = "error.location.select";
    public const string FreeAdsDisabled = "error.free_ads.disabled";
    public const string PreviewPublish = "preview.publish";
    public const string PreviewPayPublish = "preview.pay_publish";
    public const string PreviewEditTitle = "preview.edit.title";
    public const string PreviewEditText = "preview.edit.text";
    public const string PreviewEditContacts = "preview.edit.contacts";
    public const string PreviewEditMedia = "preview.edit.media";
    public const string PreviewCancel = "preview.cancel";
}
