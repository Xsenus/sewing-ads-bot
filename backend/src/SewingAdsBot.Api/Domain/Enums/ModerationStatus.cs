namespace SewingAdsBot.Api.Domain.Enums;

/// <summary>
/// Статус модерации конкретной публикации в конкретный канал.
/// </summary>
public enum ModerationStatus
{
    /// <summary>
    /// Ожидает решения модератора.
/// </summary>
    Pending = 0,

    /// <summary>
    /// Одобрено.
/// </summary>
    Approved = 1,

    /// <summary>
    /// Отклонено.
/// </summary>
    Rejected = 2
}
