using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SewingAdsBot.Api.Options;
using SewingAdsBot.Api.Services;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace SewingAdsBot.Api.Telegram;

/// <summary>
/// Фабрика TelegramBotClient.
///
/// Особенности:
/// - токен можно хранить не только в appsettings.json, но и в БД (таблица AppSettings)
///   по ключу <c>Telegram.BotToken</c>;
/// - при изменении токена фабрика пересоздаёт client и сбрасывает кеш GetMe();
/// - если токен не задан, возвращает <c>null</c> (чтобы веб‑админка могла работать
///   даже без настроенного бота).
/// </summary>
public sealed class TelegramBotClientFactory
{
    private readonly TelegramOptions _fallbackOptions;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TelegramBotClientFactory> _logger;

    private readonly object _lock = new();
    private TelegramBotClient? _client;
    private User? _me;
    private string? _currentToken;

    private static readonly TimeSpan TokenCacheTtl = TimeSpan.FromSeconds(10);
    private string? _cachedBotToken;
    private DateTime _cachedBotTokenAtUtc;
    private string? _cachedProviderToken;
    private DateTime _cachedProviderTokenAtUtc;

    /// <summary>
    /// Конструктор.
    /// </summary>
    public TelegramBotClientFactory(
        IOptions<TelegramOptions> options,
        IServiceScopeFactory scopeFactory,
        ILogger<TelegramBotClientFactory> logger)
    {
        _fallbackOptions = options.Value;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <summary>
    /// Получить актуальный bot token (из БД или из appsettings как fallback).
    /// </summary>
    public async Task<string?> GetBotTokenAsync()
    {
        if (DateTime.UtcNow - _cachedBotTokenAtUtc < TokenCacheTtl)
            return _cachedBotToken;

        var token = await ReadSettingAsync("Telegram.BotToken");
        if (string.IsNullOrWhiteSpace(token))
            token = _fallbackOptions.BotToken;

        token = NormalizeToken(token);

        _cachedBotToken = token;
        _cachedBotTokenAtUtc = DateTime.UtcNow;
        return token;
    }

    /// <summary>
    /// Получить токен провайдера платежей (для Telegram Payments).
    /// Ключ в БД: <c>Telegram.PaymentProviderToken</c>.
    /// </summary>
    public async Task<string?> GetPaymentProviderTokenAsync()
    {
        if (DateTime.UtcNow - _cachedProviderTokenAtUtc < TokenCacheTtl)
            return _cachedProviderToken;

        var token = await ReadSettingAsync("Telegram.PaymentProviderToken");
        if (string.IsNullOrWhiteSpace(token))
            token = _fallbackOptions.PaymentProviderToken;

        token = NormalizeToken(token);

        _cachedProviderToken = token;
        _cachedProviderTokenAtUtc = DateTime.UtcNow;
        return token;
    }

    /// <summary>
    /// Создать (или вернуть кешированный) TelegramBotClient.
    /// Возвращает <c>null</c>, если токен не задан.
    /// </summary>
    public async Task<TelegramBotClient?> GetClientAsync()
    {
        var token = await GetBotTokenAsync();
        if (string.IsNullOrWhiteSpace(token))
            return null;

        lock (_lock)
        {
            if (_client == null || _currentToken != token)
            {
                _client = new TelegramBotClient(token);
                _currentToken = token;
                _me = null;
                _logger.LogInformation("Telegram bot client created/replaced.");
            }

            return _client;
        }
    }

    /// <summary>
    /// Получить User бота (GetMe) с кешированием.
    /// Возвращает <c>null</c>, если токен не задан.
    /// </summary>
    public async Task<User?> GetMeCachedAsync()
    {
        if (_me != null)
            return _me;

        var bot = await GetClientAsync();
        if (bot == null)
            return null;

        _me = await bot.GetMeAsync();
        return _me;
    }

    private async Task<string?> ReadSettingAsync(string key)
    {
        using var scope = _scopeFactory.CreateScope();
        var settings = scope.ServiceProvider.GetRequiredService<SettingsService>();
        return await settings.GetAsync(key);
    }

    private static string? NormalizeToken(string? token)
    {
        token = token?.Trim();
        if (string.IsNullOrWhiteSpace(token))
            return null;

        if (token.StartsWith("PUT_", StringComparison.OrdinalIgnoreCase))
            return null;

        return token;
    }
}
