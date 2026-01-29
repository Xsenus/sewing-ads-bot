using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SewingAdsBot.Api.Data;
using SewingAdsBot.Api.Domain.Entities;

namespace SewingAdsBot.Api.Controllers;

/// <summary>
/// Список Telegram-администраторов (модераторов), которым бот отправляет заявки.
/// </summary>
[ApiController]
[Authorize]
[Route("api/admin/telegram-admins")]
public sealed class TelegramAdminsController : ControllerBase
{
    private readonly AppDbContext _db;

    /// <summary>
    /// Конструктор.
/// </summary>
    public TelegramAdminsController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Получить список модераторов.
/// </summary>
    [HttpGet]
    public async Task<ActionResult<List<TelegramAdmin>>> Get()
    {
        var list = await _db.TelegramAdmins.OrderByDescending(x => x.CreatedAtUtc).ToListAsync();
        return Ok(list);
    }

    /// <summary>
    /// Добавить модератора.
/// </summary>
    [HttpPost]
    public async Task<ActionResult> Add([FromBody] AddTelegramAdminRequest req)
    {
        var exists = await _db.TelegramAdmins.AnyAsync(x => x.TelegramUserId == req.TelegramUserId);
        if (exists)
            return BadRequest(new { message = "Этот TelegramUserId уже добавлен." });

        _db.TelegramAdmins.Add(new TelegramAdmin
        {
            TelegramUserId = req.TelegramUserId,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        return Ok();
    }

    /// <summary>
    /// Деактивировать модератора.
/// </summary>
    [HttpDelete("{telegramUserId:long}")]
    public async Task<ActionResult> Deactivate(long telegramUserId)
    {
        var item = await _db.TelegramAdmins.FirstOrDefaultAsync(x => x.TelegramUserId == telegramUserId);
        if (item == null) return NotFound();

        item.IsActive = false;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// Запрос добавления.
/// </summary>
    public sealed class AddTelegramAdminRequest
    {
        public long TelegramUserId { get; set; }
    }
}
