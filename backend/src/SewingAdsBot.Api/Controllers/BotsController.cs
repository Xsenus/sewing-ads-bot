using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SewingAdsBot.Api.Data;
using SewingAdsBot.Api.Domain.Entities;
using SewingAdsBot.Api.Domain.Enums;
using SewingAdsBot.Api.Services;
using SewingAdsBot.Api.Telegram;
using Telegram.Bot;

namespace SewingAdsBot.Api.Controllers;

/// <summary>
/// Управление Telegram-ботами.
/// </summary>
[ApiController]
[Authorize]
[Route("api/admin/bots")]
public sealed class BotsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TelegramBotMetadataService _metadataService;
    private readonly TelegramBotProfileService _profileService;
    private readonly TelegramBotRuntimeManager _runtimeManager;

    /// <summary>
    /// Конструктор.
    /// </summary>
    public BotsController(
        AppDbContext db,
        TelegramBotMetadataService metadataService,
        TelegramBotProfileService profileService,
        TelegramBotRuntimeManager runtimeManager)
    {
        _db = db;
        _metadataService = metadataService;
        _profileService = profileService;
        _runtimeManager = runtimeManager;
    }

    /// <summary>
    /// Получить список ботов.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<TelegramBotDto>>> GetAll()
    {
        var bots = await _db.TelegramBots
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync();

        return Ok(bots.Select(MapToDto).ToList());
    }

    /// <summary>
    /// Добавить нового бота по токену.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<TelegramBotDto>> Create([FromBody] CreateBotRequest request, CancellationToken ct)
    {
        request.Token = (request.Token ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(request.Token))
            return BadRequest(new { message = "Токен бота обязателен." });

        var exists = await _db.TelegramBots.AnyAsync(x => x.Token == request.Token, ct);
        if (exists)
            return Conflict(new { message = "Бот с таким токеном уже добавлен." });

        TelegramBotMetadata metadata;
        try
        {
            metadata = await _metadataService.FetchAsync(request.Token, ct);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Не удалось получить данные бота: {ex.Message}" });
        }

        var bot = new TelegramBot
        {
            Token = request.Token,
            Name = metadata.Name,
            Username = metadata.Username,
            TelegramUserId = metadata.TelegramId,
            Description = metadata.Description,
            ShortDescription = metadata.ShortDescription,
            CommandsJson = metadata.CommandsJson,
            PhotoFileId = metadata.PhotoFileId,
            PhotoFilePath = metadata.PhotoFilePath,
            Status = TelegramBotStatus.Active,
            TrackMessagesEnabled = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        if (request.CloneFromBotId.HasValue)
        {
            var sourceBot = await _db.TelegramBots.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == request.CloneFromBotId.Value, ct);

            if (sourceBot == null)
                return BadRequest(new { message = "Бот для копирования не найден." });

            bot.Name = sourceBot.Name ?? bot.Name;
            bot.Description = sourceBot.Description;
            bot.ShortDescription = sourceBot.ShortDescription;
            bot.CommandsJson = sourceBot.CommandsJson;
            bot.TrackMessagesEnabled = sourceBot.TrackMessagesEnabled;

            var commands = ParseCommands(sourceBot.CommandsJson);
            await _profileService.ApplyProfileAsync(bot.Token, new TelegramBotProfileUpdate(
                bot.Name,
                bot.Description,
                bot.ShortDescription,
                commands), ct);
        }

        _db.TelegramBots.Add(bot);
        await _db.SaveChangesAsync(ct);
        await _runtimeManager.StartBotAsync(bot, ct);

        return Ok(MapToDto(bot));
    }

    /// <summary>
    /// Приостановить бота.
    /// </summary>
    [HttpPost("{id:guid}/pause")]
    public async Task<ActionResult> Pause(Guid id, CancellationToken ct)
    {
        var bot = await _db.TelegramBots.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (bot == null)
            return NotFound(new { message = "Бот не найден." });

        bot.Status = TelegramBotStatus.Paused;
        bot.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        await _runtimeManager.StopBotAsync(bot.Id);

        return Ok(new { message = "Бот поставлен на паузу." });
    }

    /// <summary>
    /// Возобновить работу бота.
    /// </summary>
    [HttpPost("{id:guid}/resume")]
    public async Task<ActionResult> Resume(Guid id, CancellationToken ct)
    {
        var bot = await _db.TelegramBots.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (bot == null)
            return NotFound(new { message = "Бот не найден." });

        bot.Status = TelegramBotStatus.Active;
        bot.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        await _runtimeManager.StartBotAsync(bot, ct);

        return Ok(new { message = "Бот активирован." });
    }

    /// <summary>
    /// Отключить бота.
    /// </summary>
    [HttpPost("{id:guid}/disable")]
    public async Task<ActionResult> Disable(Guid id, CancellationToken ct)
    {
        var bot = await _db.TelegramBots.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (bot == null)
            return NotFound(new { message = "Бот не найден." });

        bot.Status = TelegramBotStatus.Disabled;
        bot.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        await _runtimeManager.StopBotAsync(bot.Id);

        return Ok(new { message = "Бот отключён." });
    }

    /// <summary>
    /// Перезапустить бота.
    /// </summary>
    [HttpPost("{id:guid}/restart")]
    public async Task<ActionResult> Restart(Guid id, CancellationToken ct)
    {
        var bot = await _db.TelegramBots.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (bot == null)
            return NotFound(new { message = "Бот не найден." });

        bot.Status = TelegramBotStatus.Active;
        bot.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        await _runtimeManager.RestartBotAsync(bot, ct);

        return Ok(new { message = "Бот перезапущен." });
    }

    /// <summary>
    /// Обновить метаданные бота.
    /// </summary>
    [HttpPost("{id:guid}/refresh")]
    public async Task<ActionResult<TelegramBotDto>> Refresh(Guid id, CancellationToken ct)
    {
        var bot = await _db.TelegramBots.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (bot == null)
            return NotFound(new { message = "Бот не найден." });

        TelegramBotMetadata metadata;
        try
        {
            metadata = await _metadataService.FetchAsync(bot.Token, ct);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Не удалось обновить данные: {ex.Message}" });
        }

        bot.Name = metadata.Name;
        bot.Username = metadata.Username;
        bot.TelegramUserId = metadata.TelegramId;
        bot.Description = metadata.Description;
        bot.ShortDescription = metadata.ShortDescription;
        bot.CommandsJson = metadata.CommandsJson;
        bot.PhotoFileId = metadata.PhotoFileId;
        bot.PhotoFilePath = metadata.PhotoFilePath;
        bot.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return Ok(MapToDto(bot));
    }

    /// <summary>
    /// Удалить бота.
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken ct)
    {
        var bot = await _db.TelegramBots.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (bot == null)
            return NotFound(new { message = "Бот не найден." });

        _db.TelegramBots.Remove(bot);
        await _db.SaveChangesAsync(ct);
        await _runtimeManager.StopBotAsync(bot.Id);

        return Ok(new { message = "Бот удалён." });
    }

    /// <summary>
    /// Получить список каналов, где бот является администратором.
    /// </summary>
    [HttpGet("{id:guid}/channels")]
    public async Task<ActionResult<List<BotChannelDto>>> GetBotChannels(Guid id, CancellationToken ct)
    {
        var bot = await _db.TelegramBots.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (bot == null)
            return NotFound(new { message = "Бот не найден." });

        if (string.IsNullOrWhiteSpace(bot.Token))
            return BadRequest(new { message = "У бота отсутствует токен." });

        var channels = await _db.Channels.AsNoTracking().ToListAsync(ct);
        var result = new List<BotChannelDto>();

        var client = new TelegramBotClient(bot.Token);
        foreach (var channel in channels)
        {
            try
            {
                var admins = await client.GetChatAdministratorsAsync(channel.TelegramChatId, ct);
                if (admins.Any(x => x.User.Id == bot.TelegramUserId || x.User.Username == bot.Username))
                {
                    result.Add(new BotChannelDto
                    {
                        Id = channel.Id,
                        Title = channel.Title,
                        TelegramChatId = channel.TelegramChatId,
                        TelegramUsername = channel.TelegramUsername,
                        IsActive = channel.IsActive,
                        ModerationMode = channel.ModerationMode,
                        EnableSpamFilter = channel.EnableSpamFilter,
                        SpamFilterFreeOnly = channel.SpamFilterFreeOnly,
                        RequireSubscription = channel.RequireSubscription,
                        SubscriptionChannelUsername = channel.SubscriptionChannelUsername,
                        FooterLinkText = channel.FooterLinkText,
                        FooterLinkUrl = channel.FooterLinkUrl,
                        PinnedMessageId = channel.PinnedMessageId
                    });
                }
            }
            catch
            {
                // Игнорируем ошибки отдельных каналов.
            }
        }

        return Ok(result);
    }

    /// <summary>
    /// Получить чаты/каналы, в которых замечена активность бота.
    /// </summary>
    [HttpGet("{id:guid}/chats")]
    public async Task<ActionResult<List<BotChatDto>>> GetBotChats(Guid id, CancellationToken ct)
    {
        var bot = await _db.TelegramBots.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (bot == null)
            return NotFound(new { message = "Бот не найден." });

        var chats = await _db.TelegramMessageLogs.AsNoTracking()
            .Where(x => x.TelegramBotId == id)
            .GroupBy(x => new { x.ChatId, x.ChatType, x.ChatTitle })
            .Select(g => new BotChatDto
            {
                ChatId = g.Key.ChatId,
                ChatType = g.Key.ChatType,
                ChatTitle = g.Key.ChatTitle,
                LastMessageAtUtc = g.Max(x => x.MessageDateUtc)
            })
            .OrderByDescending(x => x.LastMessageAtUtc)
            .ToListAsync(ct);

        return Ok(chats);
    }

    /// <summary>
    /// Обновить профиль и настройки бота.
    /// </summary>
    [HttpPut("{id:guid}/profile")]
    public async Task<ActionResult<TelegramBotDto>> UpdateProfile(Guid id, [FromForm] UpdateBotProfileRequest request, CancellationToken ct)
    {
        var bot = await _db.TelegramBots.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (bot == null)
            return NotFound(new { message = "Бот не найден." });

        var commands = ParseCommands(request.CommandsJson);
        await _profileService.ApplyProfileAsync(bot.Token, new TelegramBotProfileUpdate(
            request.Name,
            request.Description,
            request.ShortDescription,
            commands), ct);

        if (request.Photo != null)
            await _profileService.SetProfilePhotoAsync(bot.Token, request.Photo, ct);

        bot.Name = request.Name ?? bot.Name;
        bot.Description = request.Description ?? bot.Description;
        bot.ShortDescription = request.ShortDescription ?? bot.ShortDescription;
        if (request.CommandsJson != null)
            bot.CommandsJson = request.CommandsJson;

        if (request.TrackMessagesEnabled.HasValue)
            bot.TrackMessagesEnabled = request.TrackMessagesEnabled.Value;

        bot.UpdatedAtUtc = DateTime.UtcNow;

        if (request.Photo != null)
        {
            var metadata = await _metadataService.FetchAsync(bot.Token, ct);
            bot.PhotoFileId = metadata.PhotoFileId;
            bot.PhotoFilePath = metadata.PhotoFilePath;
        }

        await _db.SaveChangesAsync(ct);
        return Ok(MapToDto(bot));
    }

    /// <summary>
    /// Скачать аватар бота (proxy).
    /// </summary>
    [HttpGet("{id:guid}/photo")]
    public async Task<IActionResult> GetBotPhoto(Guid id, CancellationToken ct)
    {
        var bot = await _db.TelegramBots.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (bot == null || string.IsNullOrWhiteSpace(bot.PhotoFilePath))
            return NotFound();

        var stream = await _metadataService.FetchPhotoStreamAsync(bot.Token, bot.PhotoFilePath, ct);
        if (stream == null)
            return NotFound();

        return File(stream, "image/jpeg");
    }

    private static TelegramBotDto MapToDto(TelegramBot bot)
    {
        List<TelegramCommandDto>? commands = null;
        if (!string.IsNullOrWhiteSpace(bot.CommandsJson))
        {
            try
            {
                commands = JsonSerializer.Deserialize<List<TelegramCommandDto>>(bot.CommandsJson);
            }
            catch
            {
                commands = null;
            }
        }

        return new TelegramBotDto
        {
            Id = bot.Id,
            Name = bot.Name,
            Username = bot.Username,
            TelegramUserId = bot.TelegramUserId,
            Description = bot.Description,
            ShortDescription = bot.ShortDescription,
            Commands = commands,
            Status = bot.Status,
            TrackMessagesEnabled = bot.TrackMessagesEnabled,
            PhotoFileId = bot.PhotoFileId,
            CreatedAtUtc = bot.CreatedAtUtc,
            UpdatedAtUtc = bot.UpdatedAtUtc,
            LastError = bot.LastError,
            LastErrorAtUtc = bot.LastErrorAtUtc
        };
    }

    /// <summary>
    /// DTO бота для админки.
    /// </summary>
    public sealed class TelegramBotDto
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public string? Username { get; set; }
        public long TelegramUserId { get; set; }
        public string? Description { get; set; }
        public string? ShortDescription { get; set; }
        public List<TelegramCommandDto>? Commands { get; set; }
        public TelegramBotStatus Status { get; set; }
        public bool TrackMessagesEnabled { get; set; }
        public string? PhotoFileId { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
        public string? LastError { get; set; }
        public DateTime? LastErrorAtUtc { get; set; }
    }

    /// <summary>
    /// DTO команды бота.
    /// </summary>
    public sealed class TelegramCommandDto
    {
        public string? Command { get; set; }
        public string? Description { get; set; }
    }

    /// <summary>
    /// DTO канала бота.
    /// </summary>
    public sealed class BotChannelDto
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
    /// DTO чатов/каналов, где замечена активность.
    /// </summary>
    public sealed class BotChatDto
    {
        public long ChatId { get; set; }
        public string ChatType { get; set; } = string.Empty;
        public string? ChatTitle { get; set; }
        public DateTime LastMessageAtUtc { get; set; }
    }

    /// <summary>
    /// Запрос на создание бота.
    /// </summary>
    public sealed class CreateBotRequest
    {
        public string? Token { get; set; }
        public Guid? CloneFromBotId { get; set; }
    }

    /// <summary>
    /// Запрос на обновление профиля бота.
    /// </summary>
    public sealed class UpdateBotProfileRequest
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? ShortDescription { get; set; }
        public string? CommandsJson { get; set; }
        public bool? TrackMessagesEnabled { get; set; }
        public IFormFile? Photo { get; set; }
    }

    private static List<TelegramCommandPayload>? ParseCommands(string? commandsJson)
    {
        if (string.IsNullOrWhiteSpace(commandsJson))
            return null;

        try
        {
            var items = JsonSerializer.Deserialize<List<TelegramCommandDto>>(commandsJson) ?? new List<TelegramCommandDto>();
            return items
                .Where(x => !string.IsNullOrWhiteSpace(x.Command))
                .Select(x => new TelegramCommandPayload(x.Command!.Trim(), (x.Description ?? string.Empty).Trim()))
                .ToList();
        }
        catch
        {
            return null;
        }
    }
}
