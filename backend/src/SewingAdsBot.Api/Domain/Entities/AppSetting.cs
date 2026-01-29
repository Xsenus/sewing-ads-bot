using System.ComponentModel.DataAnnotations;

namespace SewingAdsBot.Api.Domain.Entities;

/// <summary>
/// Глобальные настройки, которые можно менять из админки (например цены/лимиты/URL).
/// </summary>
public sealed class AppSetting
{
    /// <summary>
    /// Внутренний идентификатор.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Ключ настройки (уникальный).
    /// </summary>
    [MaxLength(128)]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Значение настройки.
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Дата обновления.
    /// </summary>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
