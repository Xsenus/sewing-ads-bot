namespace SewingAdsBot.Api.Domain.Entities;

/// <summary>
/// Факт публикации объявления в конкретный канал (в т.ч. при «поднятии»).
/// </summary>
public sealed class AdPublication
{
    /// <summary>
    /// Внутренний идентификатор.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Идентификатор объявления.
    /// </summary>
    public Guid AdId { get; set; }

    /// <summary>
    /// Идентификатор канала.
    /// </summary>
    public Guid ChannelId { get; set; }

    /// <summary>
    /// Message ID опубликованного сообщения в канале.
    /// </summary>
    public int TelegramMessageId { get; set; }

    /// <summary>
    /// Дата публикации.
    /// </summary>
    public DateTime PublishedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Является ли публикация результатом платного поднятия (bump).
    /// </summary>
    public bool IsBump { get; set; }
}
