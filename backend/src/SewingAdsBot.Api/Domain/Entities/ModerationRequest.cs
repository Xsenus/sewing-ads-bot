using SewingAdsBot.Api.Domain.Enums;

namespace SewingAdsBot.Api.Domain.Entities;

/// <summary>
/// Заявка на модерацию объявления для конкретного канала.
/// </summary>
public sealed class ModerationRequest
{
    /// <summary>
    /// Внутренний идентификатор заявки.
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
    /// Статус модерации.
    /// </summary>
    public ModerationStatus Status { get; set; } = ModerationStatus.Pending;

    /// <summary>
    /// Причина отклонения (если есть).
    /// </summary>
    public string? RejectReason { get; set; }

    /// <summary>
    /// Telegram User ID модератора, который принял решение (если есть).
    /// </summary>
    public long? ReviewedByTelegramUserId { get; set; }

    /// <summary>
    /// Дата создания заявки.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Дата рассмотрения.
    /// </summary>
    public DateTime? ReviewedAtUtc { get; set; }
}
