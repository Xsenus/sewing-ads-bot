namespace SewingAdsBot.Api.Domain.Entities;

/// <summary>
/// Telegram-администратор (модератор), которому бот будет отправлять заявки.
/// </summary>
public sealed class TelegramAdmin
{
    /// <summary>
    /// Внутренний идентификатор.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Telegram User ID администратора.
    /// </summary>
    public long TelegramUserId { get; set; }

    /// <summary>
    /// Активен ли админ.
/// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Дата добавления.
/// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
