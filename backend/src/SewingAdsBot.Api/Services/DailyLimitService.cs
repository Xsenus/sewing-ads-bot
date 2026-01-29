using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SewingAdsBot.Api.Data;
using SewingAdsBot.Api.Domain.Entities;
using SewingAdsBot.Api.Options;

namespace SewingAdsBot.Api.Services;

/// <summary>
/// Сервис строгих календарных лимитов (не "24 часа", а именно календарный день).
/// </summary>
public sealed class DailyLimitService
{
    private readonly AppDbContext _db;
    private readonly SettingsService _settings;
    private readonly LimitOptions _limits;

    /// <summary>
    /// Конструктор.
/// </summary>
    public DailyLimitService(AppDbContext db, SettingsService settings, IOptions<LimitOptions> limits)
    {
        _db = db;
        _settings = settings;
        _limits = limits.Value;
    }

    /// <summary>
    /// Проверить, можно ли опубликовать бесплатное объявление сегодня (UTC).
    /// </summary>
    public async Task<(bool ok, int used, int limit)> CanPublishFreeAsync(long telegramUserId)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var counter = await _db.DailyCounters.FirstOrDefaultAsync(x =>
            x.TelegramUserId == telegramUserId &&
            x.DateUtc == today &&
            x.CounterKey == "FreeAdPublish");

        var used = counter?.Count ?? 0;
        var limit = await _settings.GetIntAsync("Limits.FreeAdsPerCalendarDay", _limits.FreeAdsPerCalendarDay);

        return (used < limit, used, limit);
    }

    /// <summary>
    /// Увеличить счётчик публикаций бесплатных объявлений на сегодня (UTC).
    /// </summary>
    public async Task IncrementFreePublishAsync(long telegramUserId)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var counter = await _db.DailyCounters.FirstOrDefaultAsync(x =>
            x.TelegramUserId == telegramUserId &&
            x.DateUtc == today &&
            x.CounterKey == "FreeAdPublish");

        if (counter == null)
        {
            counter = new DailyCounter
            {
                TelegramUserId = telegramUserId,
                DateUtc = today,
                CounterKey = "FreeAdPublish",
                Count = 1
            };
            _db.DailyCounters.Add(counter);
        }
        else
        {
            counter.Count += 1;
        }

        await _db.SaveChangesAsync();
    }
}
