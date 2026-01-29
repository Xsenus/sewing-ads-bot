using SewingAdsBot.Api.Telegram;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types.Enums;

namespace SewingAdsBot.Api.Services;

/// <summary>
/// Проверка подписки пользователя на канал.
/// </summary>
public sealed class SubscriptionService
{
    private readonly TelegramBotClientFactory _botFactory;
    private readonly ILogger<SubscriptionService> _logger;

    /// <summary>
    /// Конструктор.
/// </summary>
    public SubscriptionService(TelegramBotClientFactory botFactory, ILogger<SubscriptionService> logger)
    {
        _botFactory = botFactory;
        _logger = logger;
    }

    /// <summary>
    /// Проверить подписку пользователя на канал по username (без @).
    /// Возвращает false при ошибке проверки (например, у бота нет прав).
    /// </summary>
    public async Task<bool> IsSubscribedAsync(long telegramUserId, string channelUsername)
    {
        if (string.IsNullOrWhiteSpace(channelUsername))
            return true;

        var chatId = "@" + channelUsername.Trim().TrimStart('@');
        var bot = await _botFactory.GetClientAsync();
        if (bot == null)
            return false;

        try
        {
            var member = await bot.GetChatMemberAsync(chatId, telegramUserId);
            return member.Status is ChatMemberStatus.Member
                or ChatMemberStatus.Administrator
                or ChatMemberStatus.Creator;
        }
        catch (ApiRequestException ex)
        {
            _logger.LogWarning(ex, "Не удалось проверить подписку пользователя {UserId} на {Channel}.", telegramUserId, chatId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Не удалось проверить подписку пользователя {UserId} на {Channel}.", telegramUserId, chatId);
            return false;
        }
    }
}
