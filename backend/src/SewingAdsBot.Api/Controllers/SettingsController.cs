using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SewingAdsBot.Api.Data;
using SewingAdsBot.Api.Domain.Entities;
using SewingAdsBot.Api.Services;

namespace SewingAdsBot.Api.Controllers;

/// <summary>
/// Управление глобальными настройками (AppSettings).
/// </summary>
[ApiController]
[Authorize]
[Route("api/admin/settings")]
public sealed class SettingsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly SettingsService _settings;

    /// <summary>
    /// Конструктор.
/// </summary>
    public SettingsController(AppDbContext db, SettingsService settings)
    {
        _db = db;
        _settings = settings;
    }

    /// <summary>
    /// Получить все настройки.
/// </summary>
    [HttpGet]
    public async Task<ActionResult<List<AppSetting>>> GetAll()
    {
        var list = await _db.AppSettings.OrderBy(x => x.Key).ToListAsync();
        return Ok(list);
    }

    /// <summary>
    /// Установить значение настройки.
/// </summary>
    [HttpPut]
    public async Task<ActionResult> Set([FromBody] SetSettingRequest req)
    {
        await _settings.SetAsync(req.Key, req.Value);
        return Ok();
    }

    /// <summary>
    /// Запрос установки настройки.
/// </summary>
    public sealed class SetSettingRequest
    {
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }
}
