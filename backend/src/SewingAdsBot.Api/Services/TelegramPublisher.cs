using SewingAdsBot.Api.Telegram;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SewingAdsBot.Api.Data;
using SewingAdsBot.Api.Domain.Entities;
using SewingAdsBot.Api.Domain.Enums;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace SewingAdsBot.Api.Services;

/// <summary>
/// Низкоуровневый сервис отправки сообщений в Telegram-каналы и сохранения факта публикации.
/// </summary>
public sealed class TelegramPublisher
{
    private readonly AppDbContext _db;
    private readonly TelegramBotClientFactory _botFactory;
    private readonly ILogger<TelegramPublisher> _logger;

    /// <summary>
    /// Конструктор.
/// </summary>
    public TelegramPublisher(AppDbContext db, TelegramBotClientFactory botFactory, ILogger<TelegramPublisher> logger)
    {
        _db = db;
        _botFactory = botFactory;
        _logger = logger;
    }

    /// <summary>
    /// Опубликовать объявление в канал. Возвращает MessageId и ссылку (если возможно).
    /// </summary>
    public async Task<(int messageId, string? link)> PublishAsync(
        Ad ad,
        Category category,
        Channel channel,
        string text,
        bool isBump)
    {
        var bot = await _botFactory.GetClientAsync();
        if (bot == null)
            throw new InvalidOperationException("Telegram.BotToken не настроен. Задайте его в админке (Settings: Telegram.BotToken) или в appsettings.json.");

        Message msg;

        if (ad.IsPaid && ad.MediaType != MediaType.None && !string.IsNullOrWhiteSpace(ad.MediaFileId))
        {
            if (ad.MediaType == MediaType.Photo)
            {
                msg = await bot.SendPhotoAsync(
                    chatId: channel.TelegramChatId,
                    photo: InputFile.FromFileId(ad.MediaFileId),
                    caption: text,
                    parseMode: ParseMode.Html);
            }
            else
            {
                msg = await bot.SendVideoAsync(
                    chatId: channel.TelegramChatId,
                    video: InputFile.FromFileId(ad.MediaFileId),
                    caption: text,
                    parseMode: ParseMode.Html);
            }
        }
        else
        {
            msg = await bot.SendTextMessageAsync(
                chatId: channel.TelegramChatId,
                text: text,
                parseMode: ParseMode.Html);
        }

        _db.AdPublications.Add(new AdPublication
        {
            AdId = ad.Id,
            ChannelId = channel.Id,
            TelegramMessageId = msg.MessageId,
            PublishedAtUtc = DateTime.UtcNow,
            IsBump = isBump
        });

        await _db.SaveChangesAsync();

        var link = BuildMessageLink(channel, msg.MessageId);
        _logger.LogInformation("Опубликовано объявление {AdId} в канал {ChannelId}. Link={Link}", ad.Id, channel.Id, link);

        return (msg.MessageId, link);
    }

    private static string? BuildMessageLink(Channel channel, int messageId)
    {
        if (!string.IsNullOrWhiteSpace(channel.TelegramUsername))
        {
            var u = channel.TelegramUsername.Trim().TrimStart('@');
            return $"https://t.me/{u}/{messageId}";
        }

        return null;
    }
}
