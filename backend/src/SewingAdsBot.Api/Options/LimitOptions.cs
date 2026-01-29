namespace SewingAdsBot.Api.Options;

/// <summary>
/// Лимиты и ограничения бота.
/// </summary>
public sealed class LimitOptions
{
    /// <summary>
    /// Сколько бесплатных объявлений можно опубликовать за календарный день.
    /// </summary>
    public int FreeAdsPerCalendarDay { get; set; } = 1;

    /// <summary>
    /// Максимальная длина заголовка.
    /// </summary>
    public int TitleMax { get; set; } = 150;

    /// <summary>
    /// Максимальная длина текста объявления.
    /// </summary>
    public int TextMax { get; set; } = 1000;

    /// <summary>
    /// Максимальная длина контактов.
    /// </summary>
    public int ContactsMax { get; set; } = 150;
}
