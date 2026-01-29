namespace SewingAdsBot.Api.Telegram;

/// <summary>
/// Универсальная полезная нагрузка состояния для простого диалога.
/// </summary>
public sealed class FlowContext
{
    /// <summary>
    /// ID черновика объявления.
    /// </summary>
    public Guid? DraftAdId { get; set; }

    /// <summary>
    /// Текущая выбранная категория (leaf).
    /// </summary>
    public Guid? SelectedCategoryId { get; set; }

    /// <summary>
    /// Текущий родитель категории при навигации по дереву (для кнопки "Назад").
    /// </summary>
    public Guid? CategoryParentId { get; set; }

    /// <summary>
    /// Режим редактирования в предпросмотре: "title" | "text" | "contacts" | "media".
    /// </summary>
    public string? EditField { get; set; }

    /// <summary>
    /// Ключевые слова для поиска.
    /// </summary>
    public string? SearchKeywords { get; set; }
}
