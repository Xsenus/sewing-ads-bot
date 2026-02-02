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
    /// Результат проверки бесплатного размещения.
    /// </summary>
    public sealed record FreePublishAllowance(bool Ok, int Used, int Limit, string PeriodLabel, bool UsesReferralBonus, bool IsUnlimited);

    /// <summary>
    /// Проверить, можно ли опубликовать бесплатное объявление сегодня (UTC).
    /// </summary>
    public async Task<FreePublishAllowance> CanPublishFreeAsync(long telegramUserId)
    {
        var now = DateTime.UtcNow;
        var periodRaw = (await _settings.GetAsync("Limits.FreeAdsPeriod"))?.Trim();
        var period = string.IsNullOrWhiteSpace(periodRaw) ? _limits.FreeAdsPeriod : periodRaw;

        var limit = await _settings.GetIntAsync("Limits.FreeAdsPerPeriod",
            _limits.FreeAdsPerPeriod > 0 ? _limits.FreeAdsPerPeriod : _limits.FreeAdsPerCalendarDay);

        if (limit <= 0 || period.Equals("None", StringComparison.OrdinalIgnoreCase))
            return new FreePublishAllowance(true, 0, limit, "Без ограничений", UsesReferralBonus: false, IsUnlimited: true);

        var (startDate, endDate, label) = GetPeriodRange(now, period);

        var used = await _db.DailyCounters
            .Where(x => x.TelegramUserId == telegramUserId
                        && x.CounterKey == "FreeAdPublish"
                        && x.DateUtc >= startDate
                        && x.DateUtc <= endDate)
            .Select(x => (int?)x.Count)
            .SumAsync() ?? 0;

        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.TelegramUserId == telegramUserId);
        if (user?.ReferralUnlimitedPlacements == true)
            return new FreePublishAllowance(true, used, limit, label, UsesReferralBonus: false, IsUnlimited: true);

        if (used < limit)
            return new FreePublishAllowance(true, used, limit, label, UsesReferralBonus: false, IsUnlimited: false);

        if (user?.ReferralPlacementsBalance > 0)
            return new FreePublishAllowance(true, used, limit, label, UsesReferralBonus: true, IsUnlimited: false);

        return new FreePublishAllowance(false, used, limit, label, UsesReferralBonus: false, IsUnlimited: false);
    }

    /// <summary>
    /// Зарегистрировать публикацию бесплатного объявления.
    /// </summary>
    public async Task RegisterFreePublishAsync(long telegramUserId, bool usesReferralBonus)
    {
        if (usesReferralBonus)
        {
            var user = await _db.Users.FirstOrDefaultAsync(x => x.TelegramUserId == telegramUserId);
            if (user == null)
                return;

            if (user.ReferralPlacementsBalance > 0)
                user.ReferralPlacementsBalance -= 1;

            await _db.SaveChangesAsync();
            return;
        }

        await IncrementDailyCounterAsync(telegramUserId);
    }

    private async Task IncrementDailyCounterAsync(long telegramUserId)
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

    private static (DateOnly start, DateOnly end, string label) GetPeriodRange(DateTime now, string period)
    {
        var today = DateOnly.FromDateTime(now);
        var normalized = period.Trim().ToLowerInvariant();

        return normalized switch
        {
            "week" => (StartOfWeek(today), today, "неделю"),
            "month" => (new DateOnly(today.Year, today.Month, 1), today, "месяц"),
            _ => (today, today, "сутки")
        };
    }

    private static DateOnly StartOfWeek(DateOnly date)
    {
        var dayOfWeek = (int)date.DayOfWeek;
        if (dayOfWeek == 0)
            dayOfWeek = 7;

        return date.AddDays(-(dayOfWeek - 1));
    }
}
