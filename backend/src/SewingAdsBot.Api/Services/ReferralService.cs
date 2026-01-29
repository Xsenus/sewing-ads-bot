using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SewingAdsBot.Api.Data;
using SewingAdsBot.Api.Options;

namespace SewingAdsBot.Api.Services;

/// <summary>
/// Реферальная программа: привязка рефералов и начисление вознаграждения.
/// </summary>
public sealed class ReferralService
{
    private readonly AppDbContext _db;
    private readonly SettingsService _settings;
    private readonly AppOptions _options;

    /// <summary>
    /// Конструктор.
/// </summary>
    public ReferralService(AppDbContext db, SettingsService settings, IOptions<AppOptions> options)
    {
        _db = db;
        _settings = settings;
        _options = options.Value;
    }

    /// <summary>
    /// Начислить рефереру процент от оплаты (на внутренний баланс).
    /// </summary>
    public async Task ApplyReferralRewardAsync(long buyerTelegramUserId, int amountMinor, string currency)
    {
        var enabled = await _settings.GetBoolAsync("App.EnableReferralProgram", _options.EnableReferralProgram);
        if (!enabled)
            return;

        var buyer = await _db.Users.FirstOrDefaultAsync(x => x.TelegramUserId == buyerTelegramUserId);
        if (buyer?.ReferrerUserId == null)
            return;

        var referrer = await _db.Users.FirstOrDefaultAsync(x => x.Id == buyer.ReferrerUserId.Value);
        if (referrer == null)
            return;

        var percentRaw = await _settings.GetIntAsync("App.ReferralRewardPercent", _options.ReferralRewardPercent);
        var percent = Math.Clamp(percentRaw, 0, 100);

        // Простейшая модель: считаем валюту рублями, minor=копейки, переводим в decimal.
        var rewardMinor = (int)Math.Round(amountMinor * (percent / 100.0));
        var reward = rewardMinor / 100m;

        referrer.Balance += reward;
        await _db.SaveChangesAsync();
    }
}
