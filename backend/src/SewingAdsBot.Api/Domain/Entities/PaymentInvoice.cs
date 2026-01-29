using System.ComponentModel.DataAnnotations;

namespace SewingAdsBot.Api.Domain.Entities;

/// <summary>
/// Инвойс оплаты (Telegram Payments). Используется для платных объявлений и услуг (например bump).
/// </summary>
public sealed class PaymentInvoice
{
    /// <summary>
    /// Внутренний идентификатор.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Telegram User ID покупателя.
    /// </summary>
    public long TelegramUserId { get; set; }

    /// <summary>
    /// Тип покупки (например "PaidAd" или "Bump").
    /// </summary>
    [MaxLength(32)]
    public string Kind { get; set; } = string.Empty;

    /// <summary>
    /// Идентификатор объявления (если применимо).
    /// </summary>
    public Guid? AdId { get; set; }

    /// <summary>
    /// Сумма в минимальных единицах валюты (например копейки).
    /// </summary>
    public int AmountMinor { get; set; }

    /// <summary>
    /// Валюта (например "RUB").
    /// </summary>
    [MaxLength(8)]
    public string Currency { get; set; } = "RUB";

    /// <summary>
    /// Payload, который мы кладём в инвойс и потом сверяем.
    /// </summary>
    [MaxLength(128)]
    public string Payload { get; set; } = string.Empty;

    /// <summary>
    /// Оплачен ли инвойс.
    /// </summary>
    public bool IsPaid { get; set; }

    /// <summary>
    /// Telegram charge ID (если вернули).
    /// </summary>
    [MaxLength(256)]
    public string? TelegramChargeId { get; set; }

    /// <summary>
    /// Дата создания.
/// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Дата оплаты.
/// </summary>
    public DateTime? PaidAtUtc { get; set; }
}
