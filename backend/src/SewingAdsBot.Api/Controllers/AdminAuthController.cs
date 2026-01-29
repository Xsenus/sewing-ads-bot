using Microsoft.AspNetCore.Mvc;
using SewingAdsBot.Api.Services;

namespace SewingAdsBot.Api.Controllers;

/// <summary>
/// Авторизация в админку.
/// </summary>
[ApiController]
[Route("api/admin/auth")]
public sealed class AdminAuthController : ControllerBase
{
    private readonly AdminAuthService _auth;

    /// <summary>
    /// Конструктор.
/// </summary>
    public AdminAuthController(AdminAuthService auth)
    {
        _auth = auth;
    }

    /// <summary>
    /// Войти в админку и получить JWT.
/// </summary>
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest req)
    {
        var token = await _auth.LoginAsync(req.Username, req.Password);
        if (token == null)
            return Unauthorized(new { message = "Неверный логин/пароль" });

        return Ok(new LoginResponse { Token = token });
    }

    /// <summary>
    /// Запрос логина.
/// </summary>
    public sealed class LoginRequest
    {
        /// <summary>
        /// Логин.
/// </summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// Пароль.
/// </summary>
        public string Password { get; set; } = string.Empty;
    }

    /// <summary>
    /// Ответ логина.
/// </summary>
    public sealed class LoginResponse
    {
        /// <summary>
        /// JWT токен.
/// </summary>
        public string Token { get; set; } = string.Empty;
    }
}
