namespace SewingAdsBot.Api.Options;

/// <summary>
/// Настройки админки (JWT + создание первого администратора).
/// </summary>
public sealed class AdminOptions
{
    /// <summary>
    /// Логин первого администратора.
    /// </summary>
    public string InitialUsername { get; set; } = "admin";

    /// <summary>
    /// Пароль первого администратора.
    /// </summary>
    public string InitialPassword { get; set; } = "admin12345";

    /// <summary>
    /// Секрет JWT (не менее 32 символов).
    /// </summary>
    public string JwtSecret { get; set; } = "CHANGE_ME_TO_A_LONG_RANDOM_SECRET_32+";
}
