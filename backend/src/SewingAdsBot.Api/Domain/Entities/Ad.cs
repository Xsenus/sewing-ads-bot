using System.ComponentModel.DataAnnotations;
using SewingAdsBot.Api.Domain.Enums;

namespace SewingAdsBot.Api.Domain.Entities;

/// <summary>
/// Объявление пользователя.
/// </summary>
public sealed class Ad
{
    /// <summary>
    /// Внутренний идентификатор объявления.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Внутренний ID автора.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Категория объявления.
    /// </summary>
    public Guid CategoryId { get; set; }

    /// <summary>
    /// Страна (снимок из профиля на момент создания).
    /// </summary>
    [MaxLength(64)]
    public string Country { get; set; } = string.Empty;

    /// <summary>
    /// Город (снимок из профиля на момент создания).
    /// </summary>
    [MaxLength(64)]
    public string City { get; set; } = string.Empty;

    /// <summary>
    /// Заголовок объявления.
    /// </summary>
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Текст объявления.
    /// </summary>
    [MaxLength(2000)]
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Контакты (только ЛС Telegram, телефон, email).
    /// </summary>
    [MaxLength(256)]
    public string Contacts { get; set; } = string.Empty;

    /// <summary>
    /// Платное ли объявление.
    /// </summary>
    public bool IsPaid { get; set; }

    /// <summary>
    /// Тип медиа (только для платных объявлений).
    /// </summary>
    public MediaType MediaType { get; set; } = MediaType.None;

    /// <summary>
    /// Telegram file_id медиа (если есть).
    /// </summary>
    [MaxLength(256)]
    public string? MediaFileId { get; set; }

    /// <summary>
    /// Статус объявления.
    /// </summary>
    public AdStatus Status { get; set; } = AdStatus.Draft;

    /// <summary>
    /// Когда объявление создано.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Когда объявление обновлено.
    /// </summary>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Служебное поле: сколько раз делали платное поднятие (bump).
    /// </summary>
    public int BumpCount { get; set; }
}
