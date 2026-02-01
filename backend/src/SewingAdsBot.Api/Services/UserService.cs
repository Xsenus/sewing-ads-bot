using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SewingAdsBot.Api.Data;
using SewingAdsBot.Api.Domain.Entities;

namespace SewingAdsBot.Api.Services;

/// <summary>
/// Сервис работы с пользователями и их состояниями диалога.
/// </summary>
public sealed class UserService
{
    private readonly AppDbContext _db;

    /// <summary>
    /// Конструктор.
/// </summary>
    public UserService(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Получить пользователя по Telegram ID.
/// </summary>
    public Task<User?> GetByTelegramIdAsync(long telegramUserId)
        => _db.Users.FirstOrDefaultAsync(x => x.TelegramUserId == telegramUserId);

    /// <summary>
    /// Создать пользователя, если ещё не существует.
/// </summary>
    public async Task<User> GetOrCreateAsync(long telegramUserId, string? username, string? firstName)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.TelegramUserId == telegramUserId);
        if (user != null)
        {
            user.Username = username;
            user.FirstName = firstName;
            await _db.SaveChangesAsync();
            return user;
        }

        user = new User
        {
            TelegramUserId = telegramUserId,
            Username = username,
            FirstName = firstName,
            ReferralCode = GenerateReferralCode(telegramUserId)
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return user;
    }

    /// <summary>
    /// Привязать реферала к рефереру, если ещё не привязан.
/// </summary>
    public async Task<bool> TryAttachReferrerAsync(long telegramUserId, string? referralCode)
    {
        if (string.IsNullOrWhiteSpace(referralCode))
            return false;

        var user = await _db.Users.FirstOrDefaultAsync(x => x.TelegramUserId == telegramUserId);
        if (user == null)
            return false;

        if (user.ReferrerUserId != null)
            return false;

        var referrer = await _db.Users.FirstOrDefaultAsync(x => x.ReferralCode == referralCode);
        if (referrer == null || referrer.Id == user.Id)
            return false;

        user.ReferrerUserId = referrer.Id;
        await _db.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Обновить страну и город.
    /// </summary>
    public async Task UpdateLocationAsync(long telegramUserId, string? country, string? city)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.TelegramUserId == telegramUserId);
        if (user == null)
            return;

        if (country != null)
            user.Country = country;

        if (city != null)
            user.City = city;

        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Обновить язык интерфейса.
    /// </summary>
    public async Task UpdateLanguageAsync(long telegramUserId, string language)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.TelegramUserId == telegramUserId);
        if (user == null)
            return;

        user.Language = language;
        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Получить или создать состояние пользователя.
/// </summary>
    public async Task<UserState> GetOrCreateStateAsync(long telegramUserId)
    {
        var state = await _db.UserStates.FirstOrDefaultAsync(x => x.TelegramUserId == telegramUserId);
        if (state != null)
            return state;

        state = new UserState
        {
            TelegramUserId = telegramUserId,
            State = "Idle",
            PayloadJson = null
        };
        _db.UserStates.Add(state);
        await _db.SaveChangesAsync();
        return state;
    }

    /// <summary>
    /// Установить состояние и полезную нагрузку (payload) пользователя.
/// </summary>
    public async Task SetStateAsync(long telegramUserId, string state, object? payload = null)
    {
        var st = await GetOrCreateStateAsync(telegramUserId);
        st.State = state;
        st.PayloadJson = payload == null ? null : JsonSerializer.Serialize(payload);
        st.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Прочитать payload состояния пользователя.
/// </summary>
    public async Task<T?> GetStatePayloadAsync<T>(long telegramUserId)
    {
        var st = await GetOrCreateStateAsync(telegramUserId);
        if (string.IsNullOrWhiteSpace(st.PayloadJson))
            return default;

        try
        {
            return JsonSerializer.Deserialize<T>(st.PayloadJson);
        }
        catch
        {
            return default;
        }
    }

    private static string GenerateReferralCode(long telegramUserId)
    {
        // Достаточно короткий и уникальный код.
        const string alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        if (telegramUserId <= 0)
            return "0";

        var value = telegramUserId;
        Span<char> buffer = stackalloc char[32];
        var position = buffer.Length;

        while (value > 0)
        {
            var idx = (int)(value % 36);
            buffer[--position] = alphabet[idx];
            value /= 36;
        }

        return new string(buffer[position..]);
    }
}
