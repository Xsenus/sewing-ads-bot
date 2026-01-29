using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SewingAdsBot.Api.Telegram;

/// <summary>
/// Фоновая синхронизация активных ботов с базой.
/// </summary>
public sealed class TelegramBotsHostedService : BackgroundService
{
    private readonly TelegramBotRuntimeManager _runtimeManager;
    private readonly ILogger<TelegramBotsHostedService> _logger;

    /// <summary>
    /// Конструктор.
    /// </summary>
    public TelegramBotsHostedService(
        TelegramBotRuntimeManager runtimeManager,
        ILogger<TelegramBotsHostedService> logger)
    {
        _runtimeManager = runtimeManager;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _runtimeManager.SyncActiveBotsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to sync active bots.");
            }

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }
}
