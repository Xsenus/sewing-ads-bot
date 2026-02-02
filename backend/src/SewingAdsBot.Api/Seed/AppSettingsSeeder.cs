using System.Text.Json;
using Microsoft.Extensions.Options;
using SewingAdsBot.Api.Options;
using SewingAdsBot.Api.Services;

namespace SewingAdsBot.Api.Seed;

/// <summary>
/// Заполняет таблицу AppSettings значениями по умолчанию из конфигурации.
/// </summary>
public sealed class AppSettingsSeeder
{
    private readonly SettingsService _settings;
    private readonly IConfiguration _configuration;
    private readonly AppOptions _appOptions;
    private readonly LimitOptions _limitOptions;
    private readonly TelegramOptions _telegramOptions;

    public AppSettingsSeeder(
        SettingsService settings,
        IConfiguration configuration,
        IOptions<AppOptions> appOptions,
        IOptions<LimitOptions> limitOptions,
        IOptions<TelegramOptions> telegramOptions)
    {
        _settings = settings;
        _configuration = configuration;
        _appOptions = appOptions.Value;
        _limitOptions = limitOptions.Value;
        _telegramOptions = telegramOptions.Value;
    }

    public async Task SeedAsync()
    {
        var defaults = new Dictionary<string, string?>
        {
            ["Telegram.BotToken"] = _telegramOptions.BotToken,
            ["Telegram.PaymentProviderToken"] = _telegramOptions.PaymentProviderToken,
            ["App.TelegraphTariffsUrl"] = _appOptions.TelegraphTariffsUrl,
            ["App.GlobalRequiredSubscriptionChannel"] = _appOptions.GlobalRequiredSubscriptionChannel,
            ["App.RequiredSubscriptionChannels"] = _appOptions.RequiredSubscriptionChannels ?? string.Empty,
            ["App.DefaultFooterLinkText"] = _appOptions.DefaultFooterLinkText,
            ["App.DefaultFooterLinkUrl"] = _appOptions.DefaultFooterLinkUrl,
            ["App.DefaultPinText"] = _appOptions.DefaultPinText,
            ["App.EnableReferralProgram"] = _appOptions.EnableReferralProgram.ToString().ToLowerInvariant(),
            ["App.ReferralRewardPercent"] = _appOptions.ReferralRewardPercent.ToString(),
            ["Bot.Help.Text"] = "📌 Ознакомься с правилами размещения в боте.\nЕсли что-то непонятно — напиши в поддержку, мы поможем!",
            ["Bot.Help.RulesButtonText"] = "Правила",
            ["Bot.Help.RulesUrl"] = _appOptions.TelegraphTariffsUrl,
            ["Bot.Help.SupportButtonText"] = "Написать в поддержку",
            ["Bot.Help.SupportUrl"] = "https://t.me/your_support_username",
            ["Bot.Subscription.Text"] = "Чтобы продолжить, подпишитесь на каналы:\n{0}\n\nПосле подписки нажмите кнопку проверки ниже.",
            ["Bot.Subscription.CheckButtonText"] = "Проверить подписку",
            ["Bot.Referral.BalanceText"] = "Хочешь пополнить баланс? Пригласи друзей!\n\nОтправь им ссылку или нажми «Пригласить друга».\n\n🔗 Ссылка для копирования:\n{0}\n\n🎁 Награды за приглашения:\n• 1 приглашенный → +1 размещение\n• 2 → +5 размещений\n• 3 → Безлимит размещений\n\nРефералы остаются за тобой и после 3-х приглашений.\nБудут дополнительные бонусы для активных!",
            ["Bot.Referral.InviteButtonText"] = "Пригласить друга",
            ["Bot.Referral.InviteShareText"] = "@boardixrubot 👋 Привет!\n\nЕсли надо что-то продать/купить — попробуй этого бота 👇\n\n{0}",
            ["Ads.EnableFreeAds"] = GetConfig("Ads:EnableFreeAds"),
            ["Ads.FreeAllowMedia"] = GetConfig("Ads:FreeAllowMedia"),
            ["Ads.ForbidLinksInFree"] = GetConfig("Ads:ForbidLinksInFree"),
            ["Publication.ModerationMode"] = GetConfig("Publication:ModerationMode"),
            ["Post.IncludeLocationTags"] = GetConfig("Post:IncludeLocationTags"),
            ["Post.IncludeCategoryTag"] = GetConfig("Post:IncludeCategoryTag"),
            ["Post.IncludeFooterLink"] = GetConfig("Post:IncludeFooterLink"),
            ["Location.InputMode"] = GetConfig("Location:InputMode"),
            ["Location.Countries"] = SerializeConfigArray("Location:Countries"),
            ["Location.CitiesMap"] = SerializeConfigObject("Location:CitiesMap"),
            ["Limits.FreeAdsPerCalendarDay"] = _limitOptions.FreeAdsPerCalendarDay.ToString(),
            ["Limits.FreeAdsPerPeriod"] = _limitOptions.FreeAdsPerPeriod.ToString(),
            ["Limits.FreeAdsPeriod"] = _limitOptions.FreeAdsPeriod,
            ["Limits.TitleMax"] = _limitOptions.TitleMax.ToString(),
            ["Limits.TextMax"] = _limitOptions.TextMax.ToString(),
            ["Limits.ContactsMax"] = _limitOptions.ContactsMax.ToString(),
            ["PaidAdPriceMinor"] = GetConfigOrDefault("PaidAdPriceMinor", "20000"),
            ["BumpPriceMinor"] = GetConfigOrDefault("BumpPriceMinor", "5000")
        };

        foreach (var (key, value) in defaults)
        {
            var existing = await _settings.GetAsync(key);
            if (existing != null)
                continue;

            await _settings.SetAsync(key, value ?? string.Empty);
        }
    }

    private string? GetConfig(string key)
        => _configuration[key];

    private string GetConfigOrDefault(string key, string fallback)
        => string.IsNullOrWhiteSpace(_configuration[key]) ? fallback : _configuration[key]!;

    private string SerializeConfigArray(string key)
    {
        var array = _configuration.GetSection(key).Get<string[]>();
        return array == null ? string.Empty : JsonSerializer.Serialize(array);
    }

    private string SerializeConfigObject(string key)
    {
        var obj = _configuration.GetSection(key).Get<Dictionary<string, List<string>>>();
        return obj == null ? string.Empty : JsonSerializer.Serialize(obj);
    }
}
