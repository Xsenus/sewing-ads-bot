namespace SewingAdsBot.Api.Domain.Enums;

/// <summary>
/// Статус Telegram-бота в системе.
/// </summary>
public enum TelegramBotStatus
{
    /// <summary>
    /// Бот активен и принимает апдейты.
    /// </summary>
    Active = 1,

    /// <summary>
    /// Бот временно приостановлен.
    /// </summary>
    Paused = 2,

    /// <summary>
    /// Бот отключён и не должен запускаться.
    /// </summary>
    Disabled = 3
}
