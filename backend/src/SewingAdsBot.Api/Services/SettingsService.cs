using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SewingAdsBot.Api.Data;

namespace SewingAdsBot.Api.Services;

/// <summary>
/// Чтение/запись глобальных настроек из таблицы AppSettings.
/// </summary>
public sealed class SettingsService
{
    private readonly AppDbContext _db;
    private readonly IMemoryCache _cache;

    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Конструктор.
/// </summary>
    public SettingsService(AppDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    private static string CacheKey(string key) => $"appsetting:{key}";

    /// <summary>
    /// Получить строковое значение настройки.
/// </summary>
    public async Task<string?> GetAsync(string key)
    {
        if (_cache.TryGetValue(CacheKey(key), out string? cached))
            return cached;

        var value = await _db.AppSettings
            .AsNoTracking()
            .Where(x => x.Key == key)
            .Select(x => x.Value)
            .FirstOrDefaultAsync();

        _cache.Set(CacheKey(key), value, CacheTtl);
        return value;
    }

    /// <summary>
    /// Получить целочисленное значение настройки.
/// </summary>
    public async Task<int> GetIntAsync(string key, int defaultValue)
    {
        var v = await GetAsync(key);
        return int.TryParse(v, out var i) ? i : defaultValue;
    }

    /// <summary>
    /// Получить логическое значение настройки.
    /// Поддерживает значения: true/false, 1/0, yes/no, y/n.
    /// </summary>
    public async Task<bool> GetBoolAsync(string key, bool defaultValue)
    {
        var v = (await GetAsync(key))?.Trim();
        if (string.IsNullOrWhiteSpace(v))
            return defaultValue;

        if (bool.TryParse(v, out var b))
            return b;

        return v switch
        {
            "1" or "yes" or "y" or "да" => true,
            "0" or "no" or "n" or "нет" => false,
            _ => defaultValue
        };
    }

    /// <summary>
    /// Установить значение настройки.
/// </summary>
    public async Task SetAsync(string key, string value)
    {
        var item = await _db.AppSettings.FirstOrDefaultAsync(x => x.Key == key);
        if (item == null)
        {
            _db.AppSettings.Add(new Domain.Entities.AppSetting
            {
                Key = key,
                Value = value,
                UpdatedAtUtc = DateTime.UtcNow
            });
        }
        else
        {
            item.Value = value;
            item.UpdatedAtUtc = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();

        // Обновляем кэш.
        _cache.Set(CacheKey(key), value, CacheTtl);
    }
}
