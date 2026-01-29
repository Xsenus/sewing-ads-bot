using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SewingAdsBot.Api.Data;
using SewingAdsBot.Api.Domain.Entities;

namespace SewingAdsBot.Api.Controllers;

/// <summary>
/// Управление пользователями веб‑админки (логин/пароль).
/// </summary>
[ApiController]
[Authorize]
[Route("api/admin/admin-accounts")]
public sealed class AdminAccountsController : ControllerBase
{
    private readonly AppDbContext _db;

    /// <summary>
    /// Конструктор.
    /// </summary>
    public AdminAccountsController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Получить список пользователей админки.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<AdminAccountDto>>> GetAll()
    {
        var list = await _db.AdminAccounts
            .AsNoTracking()
            .OrderBy(x => x.Username)
            .Select(x => new AdminAccountDto
            {
                Id = x.Id,
                Username = x.Username,
                IsActive = x.IsActive,
                CreatedAtUtc = x.CreatedAtUtc
            })
            .ToListAsync();

        return Ok(list);
    }

    /// <summary>
    /// Создать пользователя админки.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult> Create([FromBody] CreateAdminAccountRequest req)
    {
        req.Username = (req.Username ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(req.Username))
            return BadRequest(new { message = "Username не может быть пустым." });

        if (string.IsNullOrWhiteSpace(req.Password) || req.Password.Length < 6)
            return BadRequest(new { message = "Пароль должен быть не короче 6 символов." });

        var exists = await _db.AdminAccounts.AnyAsync(x => x.Username == req.Username);
        if (exists)
            return Conflict(new { message = "Пользователь с таким логином уже существует." });

        var hash = BCrypt.Net.BCrypt.HashPassword(req.Password);

        _db.AdminAccounts.Add(new AdminAccount
        {
            Username = req.Username,
            PasswordHash = hash,
            IsActive = req.IsActive,
            CreatedAtUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        return Ok(new { message = "Создано." });
    }

    /// <summary>
    /// Сбросить пароль пользователю админки.
    /// </summary>
    [HttpPost("{id:guid}/reset-password")]
    public async Task<ActionResult> ResetPassword(Guid id, [FromBody] ResetPasswordRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Password) || req.Password.Length < 6)
            return BadRequest(new { message = "Пароль должен быть не короче 6 символов." });

        var user = await _db.AdminAccounts.FirstOrDefaultAsync(x => x.Id == id);
        if (user == null)
            return NotFound(new { message = "Не найдено." });

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Пароль обновлён." });
    }

    /// <summary>
    /// Активировать/деактивировать пользователя админки.
    /// </summary>
    [HttpPost("{id:guid}/set-active")]
    public async Task<ActionResult> SetActive(Guid id, [FromBody] SetActiveRequest req)
    {
        var user = await _db.AdminAccounts.FirstOrDefaultAsync(x => x.Id == id);
        if (user == null)
            return NotFound(new { message = "Не найдено." });

        user.IsActive = req.IsActive;
        await _db.SaveChangesAsync();
        return Ok(new { message = "Обновлено." });
    }

    /// <summary>
    /// DTO пользователя админки (без хэша пароля).
    /// </summary>
    public sealed class AdminAccountDto
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }

    /// <summary>
    /// Запрос создания пользователя админки.
    /// </summary>
    public sealed class CreateAdminAccountRequest
    {
        public string? Username { get; set; }
        public string? Password { get; set; }
        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// Запрос смены пароля.
    /// </summary>
    public sealed class ResetPasswordRequest
    {
        public string? Password { get; set; }
    }

    /// <summary>
    /// Запрос изменения активности.
    /// </summary>
    public sealed class SetActiveRequest
    {
        public bool IsActive { get; set; }
    }
}
