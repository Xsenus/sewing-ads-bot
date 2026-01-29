namespace SewingAdsBot.Api.Domain.Entities;

/// <summary>
/// Связь категория → канал (вариант C: публикация в несколько каналов).
/// </summary>
public sealed class CategoryChannel
{
    /// <summary>
    /// Идентификатор категории.
    /// </summary>
    public Guid CategoryId { get; set; }

    /// <summary>
    /// Идентификатор канала.
    /// </summary>
    public Guid ChannelId { get; set; }

    /// <summary>
    /// Включена ли публикация (можно выключать не удаляя связь).
    /// </summary>
    public bool IsEnabled { get; set; } = true;
}
