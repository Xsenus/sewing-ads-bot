using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SewingAdsBot.Api.Data;
using SewingAdsBot.Api.Domain.Entities;
using SewingAdsBot.Api.Domain.Enums;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace SewingAdsBot.Api.Telegram;

/// <summary>
/// Запускает и останавливает несколько Telegram-ботов одновременно.
/// </summary>
public sealed class TelegramBotRuntimeManager
{
    private readonly ConcurrentDictionary<Guid, BotRuntime> _runtimes = new();
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TelegramBotRuntimeManager> _logger;
    private readonly IHostApplicationLifetime _lifetime;

    /// <summary>
    /// Конструктор.
    /// </summary>
    public TelegramBotRuntimeManager(
        IServiceScopeFactory scopeFactory,
        ILogger<TelegramBotRuntimeManager> logger,
        IHostApplicationLifetime lifetime)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _lifetime = lifetime;
    }

    /// <summary>
    /// Запустить бота (если активен).
    /// </summary>
    public async Task StartBotAsync(TelegramBot bot, CancellationToken ct = default)
    {
        if (bot.Status != TelegramBotStatus.Active)
            return;

        if (string.IsNullOrWhiteSpace(bot.Token))
            return;

        if (_runtimes.TryGetValue(bot.Id, out var runtime))
        {
            if (runtime.Token == bot.Token)
                return;

            await StopBotAsync(bot.Id);
        }

        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = Array.Empty<UpdateType>()
        };

        Task HandleUpdate(ITelegramBotClient client, Update update, CancellationToken innerCt)
        {
            using var scope = _scopeFactory.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<BotUpdateHandler>();
            return handler.HandleUpdateAsync(client, update, innerCt);
        }

        Task HandleError(ITelegramBotClient client, Exception exception, CancellationToken innerCt)
        {
            _ = CaptureErrorAsync(bot.Id, exception, innerCt);

            using var scope = _scopeFactory.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<BotUpdateHandler>();
            return handler.HandleErrorAsync(client, exception, innerCt);
        }

        var client = new TelegramBotClient(bot.Token);
        var cts = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.ApplicationStopping, ct);

        client.StartReceiving(
            HandleUpdate,
            HandleError,
            receiverOptions,
            cts.Token);

        _runtimes[bot.Id] = new BotRuntime(bot.Token, client, cts);
        _logger.LogInformation("Telegram bot {BotId} started.", bot.Id);
    }

    /// <summary>
    /// Остановить бота.
    /// </summary>
    public Task StopBotAsync(Guid botId)
    {
        if (_runtimes.TryRemove(botId, out var runtime))
        {
            runtime.Cancellation.Cancel();
            runtime.Cancellation.Dispose();
            _logger.LogInformation("Telegram bot {BotId} stopped.", botId);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Перезапустить бота.
    /// </summary>
    public async Task RestartBotAsync(TelegramBot bot, CancellationToken ct = default)
    {
        await StopBotAsync(bot.Id);
        await StartBotAsync(bot, ct);
    }

    /// <summary>
    /// Синхронизировать активные боты из базы.
    /// </summary>
    public async Task SyncActiveBotsAsync(CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var activeBots = await db.TelegramBots
            .Where(x => x.Status == TelegramBotStatus.Active)
            .AsNoTracking()
            .ToListAsync(ct);

        var activeIds = activeBots.Select(x => x.Id).ToHashSet();
        foreach (var bot in activeBots)
        {
            await StartBotAsync(bot, ct);
        }

        var toStop = _runtimes.Keys.Where(id => !activeIds.Contains(id)).ToList();
        foreach (var id in toStop)
        {
            await StopBotAsync(id);
        }
    }

    private async Task CaptureErrorAsync(Guid botId, Exception exception, CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var bot = await db.TelegramBots.FirstOrDefaultAsync(x => x.Id == botId, ct);
            if (bot == null)
                return;

            bot.LastError = exception.Message;
            bot.LastErrorAtUtc = DateTime.UtcNow;
            bot.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }
        catch
        {
            // ignore errors
        }
    }

    private sealed record BotRuntime(string Token, TelegramBotClient Client, CancellationTokenSource Cancellation);
}
