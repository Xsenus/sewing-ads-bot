using System.ComponentModel.DataAnnotations;
using SewingAdsBot.Api.Domain.Enums;

namespace SewingAdsBot.Api.Domain.Entities;

/// <summary>
/// Telegram бот, подключённый к системе.
/// </summary>
public sealed class TelegramBot
{
    /// <summary>
    /// Внутренний идентификатор.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Токен Telegram Bot API.
    /// </summary>
    [MaxLength(256)]
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Имя бота.
    /// </summary>
    [MaxLength(128)]
    public string? Name { get; set; }

    /// <summary>
    /// Username бота (без @).
    /// </summary>
    [MaxLength(64)]
    public string? Username { get; set; }

    /// <summary>
    /// Telegram User ID бота.
    /// </summary>
    public long TelegramUserId { get; set; }

    /// <summary>
    /// Описание бота.
    /// </summary>
    [MaxLength(512)]
    public string? Description { get; set; }

    /// <summary>
    /// Короткое описание бота.
    /// </summary>
    [MaxLength(256)]
    public string? ShortDescription { get; set; }

    /// <summary>
    /// JSON с командами бота.
    /// </summary>
    public string? CommandsJson { get; set; }

    /// <summary>
    /// FileId основной аватарки.
    /// </summary>
    [MaxLength(256)]
    public string? PhotoFileId { get; set; }

    /// <summary>
    /// FilePath для скачивания аватарки.
    /// </summary>
    [MaxLength(512)]
    public string? PhotoFilePath { get; set; }

    /// <summary>
    /// Статус бота.
    /// </summary>
    public TelegramBotStatus Status { get; set; } = TelegramBotStatus.Active;

    /// <summary>
    /// Последняя ошибка работы (если была).
    /// </summary>
    [MaxLength(1024)]
    public string? LastError { get; set; }

    /// <summary>
    /// Время последней ошибки.
    /// </summary>
    public DateTime? LastErrorAtUtc { get; set; }

    /// <summary>
    /// Дата создания.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Дата последнего обновления.
    /// </summary>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
