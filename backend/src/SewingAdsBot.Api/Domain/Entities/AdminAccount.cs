using System.ComponentModel.DataAnnotations;

namespace SewingAdsBot.Api.Domain.Entities;

/// <summary>
/// Учётная запись для входа в веб-админку (логин/пароль).
/// </summary>
public sealed class AdminAccount
{
    /// <summary>
    /// Внутренний идентификатор.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Логин.
    /// </summary>
    [MaxLength(64)]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Хэш пароля (BCrypt).
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// Активна ли учётная запись.
    /// Если false — вход запрещён.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Дата создания.
/// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
