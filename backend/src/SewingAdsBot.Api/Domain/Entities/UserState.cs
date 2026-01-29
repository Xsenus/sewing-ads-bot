using System.ComponentModel.DataAnnotations;

namespace SewingAdsBot.Api.Domain.Entities;

/// <summary>
/// Сохранённое состояние диалога (state machine) для конкретного пользователя.
/// Нужно, чтобы пользователь мог исправлять ввод и продолжать сценарий.
/// </summary>
public sealed class UserState
{
    /// <summary>
    /// Внутренний идентификатор.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Telegram User ID (уникальный).
    /// </summary>
    public long TelegramUserId { get; set; }

    /// <summary>
    /// Текущее имя состояния.
    /// </summary>
    [MaxLength(64)]
    public string State { get; set; } = "Idle";

    /// <summary>
    /// Полезная нагрузка состояния в JSON (например DraftAdId).
    /// </summary>
    public string? PayloadJson { get; set; }

    /// <summary>
    /// Дата последнего обновления.
    /// </summary>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
