namespace SewingAdsBot.Api.Options;

/// <summary>
/// Общие настройки приложения.
/// </summary>
public sealed class AppOptions
{
    /// <summary>
    /// Ссылка на telegra.ph со списком тарифов.
    /// </summary>
    public string TelegraphTariffsUrl { get; set; } = "https://telegra.ph/";

    /// <summary>
    /// Юзернейм канала, на который пользователь должен быть подписан перед публикацией (без @).
    /// </summary>
    public string GlobalRequiredSubscriptionChannel { get; set; } = "sewing_industries";

    /// <summary>
    /// Список каналов, на которые пользователь должен быть подписан перед публикацией (через запятую, без @).
    /// </summary>
    public string RequiredSubscriptionChannels { get; set; } = string.Empty;

    /// <summary>
    /// Текст системной ссылки, добавляемой внизу поста.
    /// </summary>
    public string DefaultFooterLinkText { get; set; } = "Швейные производства • Объявления";

    /// <summary>
    /// URL системной ссылки, добавляемой внизу поста.
    /// </summary>
    public string DefaultFooterLinkUrl { get; set; } = "https://t.me/sewing_industries";

    /// <summary>
    /// Текст сообщения, которое бот отправляет в канал для закрепа с кнопкой «ОПУБЛИКОВАТЬ».
    /// </summary>
    public string DefaultPinText { get; set; } = "Нажмите кнопку ниже, чтобы опубликовать объявление.";

    /// <summary>
    /// Включить реферальную программу.
    /// </summary>
    public bool EnableReferralProgram { get; set; } = true;

    /// <summary>
    /// Процент вознаграждения рефереру от суммы оплаты (0..100).
    /// </summary>
    public int ReferralRewardPercent { get; set; } = 10;
}
