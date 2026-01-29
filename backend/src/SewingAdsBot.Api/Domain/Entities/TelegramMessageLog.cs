using System.ComponentModel.DataAnnotations;

namespace SewingAdsBot.Api.Domain.Entities;

/// <summary>
/// Лог входящих сообщений для Telegram-бота.
/// </summary>
public sealed class TelegramMessageLog
{
    /// <summary>
    /// Внутренний идентификатор.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Идентификатор бота.
    /// </summary>
    public Guid TelegramBotId { get; set; }

    /// <summary>
    /// Telegram User ID бота.
    /// </summary>
    public long TelegramBotUserId { get; set; }

    /// <summary>
    /// Chat ID.
    /// </summary>
    public long ChatId { get; set; }

    /// <summary>
    /// Тип чата.
    /// </summary>
    [MaxLength(32)]
    public string ChatType { get; set; } = string.Empty;

    /// <summary>
    /// Название чата/канала.
    /// </summary>
    [MaxLength(256)]
    public string? ChatTitle { get; set; }

    /// <summary>
    /// ID сообщения.
    /// </summary>
    public int MessageId { get; set; }

    /// <summary>
    /// Дата сообщения (UTC).
    /// </summary>
    public DateTime MessageDateUtc { get; set; }

    /// <summary>
    /// Telegram User ID отправителя (если есть).
    /// </summary>
    public long? FromUserId { get; set; }

    /// <summary>
    /// Username отправителя.
    /// </summary>
    [MaxLength(64)]
    public string? FromUsername { get; set; }

    /// <summary>
    /// Имя отправителя.
    /// </summary>
    [MaxLength(128)]
    public string? FromFirstName { get; set; }

    /// <summary>
    /// Текст сообщения.
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// Подпись к медиа.
    /// </summary>
    public string? Caption { get; set; }

    /// <summary>
    /// Признак пересланного сообщения.
    /// </summary>
    public bool IsForwarded { get; set; }

    /// <summary>
    /// Переслано от пользователя.
    /// </summary>
    public long? ForwardFromUserId { get; set; }

    /// <summary>
    /// Переслано из чата.
    /// </summary>
    public long? ForwardFromChatId { get; set; }

    /// <summary>
    /// JSON сырых данных Telegram.
    /// </summary>
    public string? PayloadJson { get; set; }

    /// <summary>
    /// Дата создания записи.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
