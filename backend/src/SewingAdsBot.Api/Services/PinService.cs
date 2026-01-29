using SewingAdsBot.Api.Telegram;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SewingAdsBot.Api.Data;
using SewingAdsBot.Api.Domain.Entities;
using SewingAdsBot.Api.Options;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace SewingAdsBot.Api.Services;

/// <summary>
/// Сервис закрепа/открепа кнопки «ОПУБЛИКОВАТЬ» в канале.
/// </summary>
public sealed class PinService
{
    private readonly AppDbContext _db;
    private readonly SettingsService _settings;
    private readonly TelegramBotClientFactory _botFactory;
    private readonly AppOptions _appOptions;

    /// <summary>
    /// Конструктор.
/// </summary>
    public PinService(AppDbContext db, SettingsService settings, TelegramBotClientFactory botFactory, IOptions<AppOptions> appOptions)
    {
        _db = db;
        _settings = settings;
        _botFactory = botFactory;
        _appOptions = appOptions.Value;
    }

    /// <summary>
    /// Закрепить системное сообщение с кнопкой «ОПУБЛИКОВАТЬ» в заданном канале.
    /// </summary>
    public async Task<(bool ok, string message)> PinPublishButtonAsync(Guid channelId)
    {
        var channel = await _db.Channels.FirstOrDefaultAsync(x => x.Id == channelId);
        if (channel == null)
            return (false, "Канал не найден.");

        var bot = await _botFactory.GetClientAsync();
        if (bot == null)
            throw new InvalidOperationException("Telegram.BotToken не настроен. Не могу закрепить сообщение.");
        var me = await _botFactory.GetMeCachedAsync();
        var botUsername = me?.Username;

        if (string.IsNullOrWhiteSpace(botUsername))
            return (false, "Не удалось определить username бота.");

        var url = $"https://t.me/{botUsername}?start=publish";

        var kb = new InlineKeyboardMarkup(
            InlineKeyboardButton.WithUrl("ОПУБЛИКОВАТЬ", url)
        );

        var pinText = (await _settings.GetAsync("App.DefaultPinText"))?.Trim();
        if (string.IsNullOrWhiteSpace(pinText))
            pinText = _appOptions.DefaultPinText;

        var msg = await bot.SendTextMessageAsync(
            chatId: channel.TelegramChatId,
            text: pinText,
            replyMarkup: kb);

        await bot.PinChatMessageAsync(chatId: channel.TelegramChatId, messageId: msg.MessageId, disableNotification: true);

        channel.PinnedMessageId = msg.MessageId;
        await _db.SaveChangesAsync();

        return (true, "Закреплено.");
    }

    /// <summary>
    /// Открепить системное сообщение (если бот его закреплял) в заданном канале.
    /// </summary>
    public async Task<(bool ok, string message)> UnpinPublishButtonAsync(Guid channelId)
    {
        var channel = await _db.Channels.FirstOrDefaultAsync(x => x.Id == channelId);
        if (channel == null)
            return (false, "Канал не найден.");

        var bot = await _botFactory.GetClientAsync();
        if (bot == null)
            throw new InvalidOperationException("Telegram.BotToken не настроен. Не могу открепить сообщение.");

        if (channel.PinnedMessageId.HasValue)
        {
            await bot.UnpinChatMessageAsync(chatId: channel.TelegramChatId, messageId: channel.PinnedMessageId.Value);
            channel.PinnedMessageId = null;
            await _db.SaveChangesAsync();
            return (true, "Откреплено.");
        }

        // На всякий случай - открепляем последнее закреплённое
        await bot.UnpinChatMessageAsync(channel.TelegramChatId);
        return (true, "Откреплено (последнее закреплённое сообщение).");
    }
}
