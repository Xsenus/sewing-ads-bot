namespace SewingAdsBot.Api.Options;

/// <summary>
/// Настройки Telegram-бота и платежей.
/// </summary>
public sealed class TelegramOptions
{
    /// <summary>
    /// Токен бота (BotFather).
    /// </summary>
    public string BotToken { get; set; } = string.Empty;

    /// <summary>
    /// Provider Token для Telegram Payments (ЮKassa/Stripe/CloudPayments и т.д.).
    /// </summary>
    public string PaymentProviderToken { get; set; } = string.Empty;
}
