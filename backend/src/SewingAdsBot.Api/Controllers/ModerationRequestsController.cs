using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SewingAdsBot.Api.Data;
using SewingAdsBot.Api.Domain.Enums;
using SewingAdsBot.Api.Services;

namespace SewingAdsBot.Api.Controllers;

/// <summary>
/// Просмотр очереди модерации (для админки).
/// </summary>
[ApiController]
[Authorize]
[Route("api/admin/moderation")]
public sealed class ModerationRequestsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly PostFormatter _formatter;
    private readonly ModerationService _moderation;

    /// <summary>
    /// Конструктор.
/// </summary>
    public ModerationRequestsController(AppDbContext db, PostFormatter formatter, ModerationService moderation)
    {
        _db = db;
        _formatter = formatter;
        _moderation = moderation;
    }

    /// <summary>
    /// Получить заявки на модерацию (по умолчанию Pending).
    /// </summary>
    [HttpGet("requests")]
    public async Task<ActionResult> Get([FromQuery] ModerationStatus? status = null)
    {
        var st = status ?? ModerationStatus.Pending;

        var list = await _db.ModerationRequests
            .Where(x => x.Status == st)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(200)
            .ToListAsync();

        return Ok(list);
    }

    /// <summary>
    /// Получить текстовый предпросмотр заявки (HTML), как он приходит модераторам в Telegram.
    /// </summary>
    [HttpGet("requests/{id:guid}/preview")]
    public async Task<ActionResult<string>> Preview(Guid id)
    {
        var req = await _db.ModerationRequests.FirstOrDefaultAsync(x => x.Id == id);
        if (req == null)
            return NotFound();

        var ad = await _db.Ads.FirstOrDefaultAsync(x => x.Id == req.AdId);
        if (ad == null)
            return NotFound();

        var category = await _db.Categories.FirstOrDefaultAsync(x => x.Id == ad.CategoryId);
        var channel = await _db.Channels.FirstOrDefaultAsync(x => x.Id == req.ChannelId);
        if (category == null || channel == null)
            return NotFound();

        var preview = _formatter.BuildModerationPreview(ad, category, channel);
        return Content(preview, "text/plain");
    }

    /// <summary>
    /// Одобрить заявку из админки и опубликовать пост.
    /// </summary>
    [HttpPost("requests/{id:guid}/approve")]
    public async Task<ActionResult> Approve(Guid id)
    {
        // В веб-админке нет TelegramUserId модератора, поэтому пишем null.
        var (ok, msg) = await _moderation.ApproveAsync(id, adminTelegramUserId: null);
        return ok ? Ok(new { message = msg }) : BadRequest(new { message = msg });
    }

    /// <summary>
    /// Отклонить заявку из админки.
    /// </summary>
    [HttpPost("requests/{id:guid}/reject")]
    public async Task<ActionResult> Reject(Guid id, [FromBody] RejectRequest req)
    {
        var (ok, msg) = await _moderation.RejectAsync(id, adminTelegramUserId: null, reason: req.Reason);
        return ok ? Ok(new { message = msg }) : BadRequest(new { message = msg });
    }

    /// <summary>
    /// Тело запроса отклонения.
    /// </summary>
    public sealed class RejectRequest
    {
        /// <summary>
        /// Причина отклонения.
        /// </summary>
        public string? Reason { get; set; }
    }
}
