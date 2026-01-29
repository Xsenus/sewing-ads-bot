namespace SewingAdsBot.Api.Domain.Entities;

/// <summary>
/// Счётчик действий по календарным дням (строгий лимит "1 раз в сутки").
/// </summary>
public sealed class DailyCounter
{
    /// <summary>
    /// Внутренний идентификатор.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Telegram User ID.
    /// </summary>
    public long TelegramUserId { get; set; }

    /// <summary>
    /// Календарная дата (UTC).
    /// </summary>
    public DateOnly DateUtc { get; set; }

    /// <summary>
    /// Тип счётчика (например "FreeAdPublish").
    /// </summary>
    public string CounterKey { get; set; } = "FreeAdPublish";

    /// <summary>
    /// Значение счётчика.
    /// </summary>
    public int Count { get; set; }
}
