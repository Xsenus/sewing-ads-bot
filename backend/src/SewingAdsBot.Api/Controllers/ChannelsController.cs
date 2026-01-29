using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SewingAdsBot.Api.Data;
using SewingAdsBot.Api.Domain.Entities;
using SewingAdsBot.Api.Domain.Enums;
using SewingAdsBot.Api.Services;

namespace SewingAdsBot.Api.Controllers;

/// <summary>
/// Управление каналами и закрепом кнопки «ОПУБЛИКОВАТЬ».
/**/
/// </summary>
[ApiController]
[Authorize]
[Route("api/admin/channels")]
public sealed class ChannelsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ChannelService _channels;
    private readonly PinService _pin;

    /// <summary>
    /// Конструктор.
/// </summary>
    public ChannelsController(AppDbContext db, ChannelService channels, PinService pin)
    {
        _db = db;
        _channels = channels;
        _pin = pin;
    }

    /// <summary>
    /// Получить список каналов.
/// </summary>
    [HttpGet]
    public async Task<ActionResult<List<ChannelDto>>> GetAll()
    {
        var list = await _db.Channels.OrderBy(x => x.Title).ToListAsync();
        return Ok(list.Select(ToDto).ToList());
    }

    /// <summary>
    /// Создать канал.
/// </summary>
    [HttpPost]
    public async Task<ActionResult<ChannelDto>> Create([FromBody] UpsertChannelRequest req)
    {
        var ch = new Channel
        {
            Title = req.Title,
            TelegramChatId = req.TelegramChatId,
            TelegramUsername = req.TelegramUsername,
            IsActive = req.IsActive,
            ModerationMode = req.ModerationMode,
            EnableSpamFilter = req.EnableSpamFilter,
            SpamFilterFreeOnly = req.SpamFilterFreeOnly,
            RequireSubscription = req.RequireSubscription,
            SubscriptionChannelUsername = req.SubscriptionChannelUsername,
            FooterLinkText = req.FooterLinkText,
            FooterLinkUrl = req.FooterLinkUrl
        };

        var saved = await _channels.EnsureAsync(ch);
        return Ok(ToDto(saved));
    }

    /// <summary>
    /// Обновить канал.
/// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ChannelDto>> Update(Guid id, [FromBody] UpsertChannelRequest req)
    {
        var ch = await _db.Channels.FirstOrDefaultAsync(x => x.Id == id);
        if (ch == null) return NotFound();

        ch.Title = req.Title;
        ch.TelegramChatId = req.TelegramChatId;
        ch.TelegramUsername = req.TelegramUsername;
        ch.IsActive = req.IsActive;
        ch.ModerationMode = req.ModerationMode;
        ch.EnableSpamFilter = req.EnableSpamFilter;
        ch.SpamFilterFreeOnly = req.SpamFilterFreeOnly;
        ch.RequireSubscription = req.RequireSubscription;
        ch.SubscriptionChannelUsername = req.SubscriptionChannelUsername;
        ch.FooterLinkText = req.FooterLinkText;
        ch.FooterLinkUrl = req.FooterLinkUrl;

        await _channels.UpdateAsync(ch);
        return Ok(ToDto(ch));
    }

    /// <summary>
    /// Деактивировать канал.
/// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Deactivate(Guid id)
    {
        var ch = await _db.Channels.FirstOrDefaultAsync(x => x.Id == id);
        if (ch == null) return NotFound();

        ch.IsActive = false;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// Закрепить кнопку «ОПУБЛИКОВАТЬ» (системное сообщение) в канале.
/// </summary>
    [HttpPost("{id:guid}/pin")]
    public async Task<ActionResult> Pin(Guid id)
    {
        var (ok, msg) = await _pin.PinPublishButtonAsync(id);
        return ok ? Ok(new { message = msg }) : BadRequest(new { message = msg });
    }

    /// <summary>
    /// Открепить кнопку «ОПУБЛИКОВАТЬ» в канале.
/// </summary>
    [HttpPost("{id:guid}/unpin")]
    public async Task<ActionResult> Unpin(Guid id)
    {
        var (ok, msg) = await _pin.UnpinPublishButtonAsync(id);
        return ok ? Ok(new { message = msg }) : BadRequest(new { message = msg });
    }

    /// <summary>
    /// DTO канала.
/// </summary>
    public sealed class ChannelDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public long TelegramChatId { get; set; }
        public string? TelegramUsername { get; set; }
        public bool IsActive { get; set; }
        public ChannelModerationMode ModerationMode { get; set; }
        public bool EnableSpamFilter { get; set; }
        public bool SpamFilterFreeOnly { get; set; }
        public bool RequireSubscription { get; set; }
        public string? SubscriptionChannelUsername { get; set; }
        public string FooterLinkText { get; set; } = string.Empty;
        public string FooterLinkUrl { get; set; } = string.Empty;
        public int? PinnedMessageId { get; set; }
    }

    /// <summary>
    /// Запрос создания/обновления канала.
/// </summary>
    public sealed class UpsertChannelRequest
    {
        public string Title { get; set; } = string.Empty;
        public long TelegramChatId { get; set; }
        public string? TelegramUsername { get; set; }
        public bool IsActive { get; set; } = true;
        public ChannelModerationMode ModerationMode { get; set; } = ChannelModerationMode.Auto;
        public bool EnableSpamFilter { get; set; } = true;
        public bool SpamFilterFreeOnly { get; set; } = true;
        public bool RequireSubscription { get; set; } = true;
        public string? SubscriptionChannelUsername { get; set; }
        public string FooterLinkText { get; set; } = "Швейные производства • Объявления";
        public string FooterLinkUrl { get; set; } = "https://t.me/sewing_industries";
    }

    private static ChannelDto ToDto(Channel c) => new()
    {
        Id = c.Id,
        Title = c.Title,
        TelegramChatId = c.TelegramChatId,
        TelegramUsername = c.TelegramUsername,
        IsActive = c.IsActive,
        ModerationMode = c.ModerationMode,
        EnableSpamFilter = c.EnableSpamFilter,
        SpamFilterFreeOnly = c.SpamFilterFreeOnly,
        RequireSubscription = c.RequireSubscription,
        SubscriptionChannelUsername = c.SubscriptionChannelUsername,
        FooterLinkText = c.FooterLinkText,
        FooterLinkUrl = c.FooterLinkUrl,
        PinnedMessageId = c.PinnedMessageId
    };
}
