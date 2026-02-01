using System.ComponentModel.DataAnnotations;

namespace SewingAdsBot.Api.Domain.Entities;

/// <summary>
/// Пользователь Telegram, который взаимодействует с ботом.
/// </summary>
public sealed class User
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
    /// Юзернейм Telegram (без @), может быть пустым.
    /// </summary>
    [MaxLength(64)]
    public string? Username { get; set; }

    /// <summary>
    /// Имя пользователя (как вернул Telegram).
    /// </summary>
    [MaxLength(128)]
    public string? FirstName { get; set; }

    /// <summary>
    /// Страна (вводится пользователем вручную).
    /// </summary>
    [MaxLength(64)]
    public string? Country { get; set; }

    /// <summary>
    /// Город (вводится пользователем вручную).
    /// </summary>
    [MaxLength(64)]
    public string? City { get; set; }

    /// <summary>
    /// Язык интерфейса бота (например "ru" или "en").
    /// </summary>
    [MaxLength(8)]
    public string? Language { get; set; }

    /// <summary>
    /// Текущий баланс (если вы решите использовать внутренний баланс).
    /// </summary>
    public decimal Balance { get; set; }

    /// <summary>
    /// Реферальный код пользователя (для /start ref_xxx).
    /// </summary>
    [MaxLength(32)]
    public string ReferralCode { get; set; } = string.Empty;

    /// <summary>
    /// Внутренний ID пользователя-реферера (кто пригласил).
    /// </summary>
    public Guid? ReferrerUserId { get; set; }

    /// <summary>
    /// Дата регистрации в боте.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
