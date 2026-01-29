using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace SewingAdsBot.Api.Telegram;

/// <summary>
/// HostedService, который запускает long polling и обрабатывает обновления через scoped BotUpdateHandler.
/// </summary>
public sealed class TelegramBotHostedService : BackgroundService
{
    private readonly TelegramBotClientFactory _botFactory;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TelegramBotHostedService> _logger;

    /// <summary>
    /// Конструктор.
/// </summary>
    public TelegramBotHostedService(
        TelegramBotClientFactory botFactory,
        IServiceScopeFactory scopeFactory,
        ILogger<TelegramBotHostedService> logger)
    {
        _botFactory = botFactory;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = Array.Empty<UpdateType>()
        };

        Task HandleUpdate(ITelegramBotClient client, Update update, CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<BotUpdateHandler>();
            return handler.HandleUpdateAsync(client, update, ct);
        }

        Task HandleError(ITelegramBotClient client, Exception exception, CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<BotUpdateHandler>();
            return handler.HandleErrorAsync(client, exception, ct);
        }

        string? activeToken = null;
        CancellationTokenSource? receiverCts = null;

        while (!stoppingToken.IsCancellationRequested)
        {
            var token = await _botFactory.GetBotTokenAsync();

            // Токен не задан: бот не стартуем, но API/админка продолжают работать.
            if (string.IsNullOrWhiteSpace(token))
            {
                if (activeToken != null)
                {
                    receiverCts?.Cancel();
                    receiverCts?.Dispose();
                    receiverCts = null;
                    activeToken = null;
                    _logger.LogWarning("Telegram bot stopped because token is missing.");
                }

                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                continue;
            }

            // Если токен тот же — просто ждём.
            if (activeToken == token)
            {
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                continue;
            }

            // Токен изменился (или бот ещё не запускали) — перезапускаем receiving.
            receiverCts?.Cancel();
            receiverCts?.Dispose();
            receiverCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);

            var bot = await _botFactory.GetClientAsync();
            if (bot == null)
            {
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                continue;
            }

            var me = await _botFactory.GetMeCachedAsync();
            if (me != null)
                _logger.LogInformation("Telegram bot started: @{Username} ({Id})", me.Username, me.Id);
            else
                _logger.LogInformation("Telegram bot started.");

            bot.StartReceiving(
                updateHandler: HandleUpdate,
                pollingErrorHandler: HandleError,
                receiverOptions: receiverOptions,
                cancellationToken: receiverCts.Token);

            activeToken = token;

            // Небольшая пауза, чтобы не входить в tight-loop.
            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }
}
