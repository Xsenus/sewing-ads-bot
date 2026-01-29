using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SewingAdsBot.Api.Data;
using SewingAdsBot.Api.Domain.Entities;
using SewingAdsBot.Api.Options;

namespace SewingAdsBot.Api.Services;

/// <summary>
/// Аутентификация админки (логин/пароль + JWT).
/// </summary>
public sealed class AdminAuthService
{
    private readonly AppDbContext _db;
    private readonly AdminOptions _options;

    /// <summary>
    /// Конструктор.
/// </summary>
    public AdminAuthService(AppDbContext db, IOptions<AdminOptions> options)
    {
        _db = db;
        _options = options.Value;
    }

    /// <summary>
    /// Создать первого администратора (если таблица пуста).
    /// </summary>
    public async Task EnsureInitialAdminAsync()
    {
        var any = await _db.AdminAccounts.AnyAsync();
        if (any) return;

        var hash = BCrypt.Net.BCrypt.HashPassword(_options.InitialPassword);

        _db.AdminAccounts.Add(new AdminAccount
        {
            Username = _options.InitialUsername,
            PasswordHash = hash,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Попытка логина. Возвращает JWT при успехе.
/// </summary>
    public async Task<string?> LoginAsync(string username, string password)
    {
        var user = await _db.AdminAccounts.FirstOrDefaultAsync(x => x.Username == username);
        if (user == null)
            return null;

        if (!user.IsActive)
            return null;

        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            return null;

        return GenerateJwt(username);
    }

    private string GenerateJwt(string username)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.JwtSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, username),
            new("role", "admin")
        };

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
