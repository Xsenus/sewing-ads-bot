namespace SewingAdsBot.Api.Telegram;

/// <summary>
/// Список состояний диалога.
/// Используем строки, чтобы проще смотреть в БД.
/// </summary>
public static class BotStates
{
    /// <summary>
    /// Ничего не ждём (главное меню).
    /// </summary>
    public const string Idle = "Idle";

    /// <summary>
    /// Ожидаем выбор языка.
    /// </summary>
    public const string ChoosingLanguage = "ChoosingLanguage";

    /// <summary>
    /// Ожидаем выбор языка из профиля.
    /// </summary>
    public const string ChoosingLanguageProfile = "ChoosingLanguageProfile";

    /// <summary>
    /// Ожидаем подписку на обязательные каналы.
    /// </summary>
    public const string AwaitingSubscription = "AwaitingSubscription";

    /// <summary>
    /// Меню профиля.
    /// </summary>
    public const string ProfileMenu = "ProfileMenu";

    /// <summary>
    /// Ожидаем ввод страны.
    /// </summary>
    public const string AwaitCountry = "AwaitCountry";

    /// <summary>
    /// Ожидаем ввод города.
    /// </summary>
    public const string AwaitCity = "AwaitCity";

    /// <summary>
    /// Создание объявления: выбор категории.
    /// </summary>
    public const string Creating_SelectCategory = "Creating_SelectCategory";

    /// <summary>
    /// Создание объявления: выбор типа (free/paid).
    /// </summary>
    public const string Creating_SelectType = "Creating_SelectType";

    /// <summary>
    /// Создание объявления: ожидание заголовка.
    /// </summary>
    public const string Creating_AwaitTitle = "Creating_AwaitTitle";

    /// <summary>
    /// Создание объявления: ожидание текста.
    /// </summary>
    public const string Creating_AwaitText = "Creating_AwaitText";

    /// <summary>
    /// Создание объявления: ожидание контактов.
    /// </summary>
    public const string Creating_AwaitContacts = "Creating_AwaitContacts";

    /// <summary>
    /// Создание объявления: ожидание медиа (только платные).
    /// </summary>
    public const string Creating_AwaitMedia = "Creating_AwaitMedia";

    /// <summary>
    /// Создание объявления: предпросмотр.
    /// </summary>
    public const string Creating_Preview = "Creating_Preview";

    /// <summary>
    /// Поиск: выбор категории.
    /// </summary>
    public const string Searching_SelectCategory = "Searching_SelectCategory";

    /// <summary>
    /// Поиск: ввод ключевых слов.
    /// </summary>
    public const string Searching_AwaitKeywords = "Searching_AwaitKeywords";
}
