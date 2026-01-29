using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SewingAdsBot.Api.Data;
using SewingAdsBot.Api.Domain.Enums;
using SewingAdsBot.Api.Options;

namespace SewingAdsBot.Api.Services;

/// <summary>
/// Публикация объявлений в каналы (поддержка нескольких каналов для одной категории — вариант C).
/// </summary>
public sealed class PublicationService
{
    private readonly AppDbContext _db;
    private readonly SettingsService _settings;
    private readonly ChannelService _channelService;
    private readonly CategoryService _categoryService;
    private readonly UserService _userService;
    private readonly SubscriptionService _subscriptionService;
    private readonly LinkGuardService _linkGuard;
    private readonly DailyLimitService _dailyLimit;
    private readonly ModerationService _moderationService;
    private readonly TelegramPublisher _publisher;
    private readonly PostFormatter _formatter;
    private readonly AppOptions _appOptions;
    private readonly ILogger<PublicationService> _logger;

    /// <summary>
    /// Конструктор.
/// </summary>
    public PublicationService(
        AppDbContext db,
        SettingsService settings,
        ChannelService channelService,
        CategoryService categoryService,
        UserService userService,
        SubscriptionService subscriptionService,
        LinkGuardService linkGuard,
        DailyLimitService dailyLimit,
        ModerationService moderationService,
        TelegramPublisher publisher,
        PostFormatter formatter,
        IOptions<AppOptions> appOptions,
        ILogger<PublicationService> logger)
    {
        _db = db;
        _settings = settings;
        _channelService = channelService;
        _categoryService = categoryService;
        _userService = userService;
        _subscriptionService = subscriptionService;
        _linkGuard = linkGuard;
        _dailyLimit = dailyLimit;
        _moderationService = moderationService;
        _publisher = publisher;
        _formatter = formatter;
        _appOptions = appOptions.Value;
        _logger = logger;
    }

    /// <summary>
    /// Принять объявление на публикацию:
    /// - проверка лимитов/подписки/спама
    /// - публикация в auto-каналы
    /// - создание заявок на модерацию для moderation-каналов
    /// </summary>
    public async Task<PublicationResult> SubmitAsync(long telegramUserId, Guid adId)
    {
        var user = await _userService.GetByTelegramIdAsync(telegramUserId);
        if (user == null)
            return new PublicationResult { Ok = false, Message = "Пользователь не найден." };

        var ad = await _db.Ads.FirstOrDefaultAsync(x => x.Id == adId && x.UserId == user.Id);
        if (ad == null)
            return new PublicationResult { Ok = false, Message = "Объявление не найдено." };

        var cat = await _categoryService.GetByIdAsync(ad.CategoryId);
        if (cat == null)
            return new PublicationResult { Ok = false, Message = "Категория не найдена." };

        // 1) Проверка подписки (глобальная)
        var globalSub = (await _settings.GetAsync("App.GlobalRequiredSubscriptionChannel"))?.Trim();
        if (string.IsNullOrWhiteSpace(globalSub))
            globalSub = _appOptions.GlobalRequiredSubscriptionChannel;

        if (!string.IsNullOrWhiteSpace(globalSub))
        {
            var okSub = await _subscriptionService.IsSubscribedAsync(telegramUserId, globalSub);
            if (!okSub)
            {
                return new PublicationResult
                {
                    Ok = false,
                    Message = $"Перед публикацией подпишитесь на канал: https://t.me/{globalSub}"
                };
            }
        }

        // 2) Лимит бесплатных (календарный день)
        if (!ad.IsPaid)
        {
            var (ok, used, limit) = await _dailyLimit.CanPublishFreeAsync(telegramUserId);
            if (!ok)
            {
                return new PublicationResult
                {
                    Ok = false,
                    Message = $"Лимит бесплатных объявлений: {limit} в сутки. Сегодня уже использовано: {used}."
                };
            }
        }

        // 3) Запрет ссылок в бесплатной версии (глобально)
        if (!ad.IsPaid)
        {
            if (_linkGuard.ContainsForbiddenLinks(ad.Title) ||
                _linkGuard.ContainsForbiddenLinks(ad.Text) ||
                _linkGuard.ContainsForbiddenLinks(ad.Contacts))
            {
                return new PublicationResult
                {
                    Ok = false,
                    Message = "В бесплатной версии запрещены ссылки на каналы/группы/сайты/ботов/сервисы/Google таблицы. " +
                              "Выберите «Платное объявление», чтобы добавить ссылки."
                };
            }
        }

        // 4) Получаем список каналов для категории (вариант C)
        var channels = await _channelService.GetChannelsForCategoryAsync(ad.CategoryId);
        if (channels.Count == 0)
        {
            return new PublicationResult
            {
                Ok = false,
                Message = "Для этой категории не настроены каналы публикации. Обратитесь к администратору."
            };
        }

        // 5) Помечаем объявление как принятое
        ad.Status = Domain.Enums.AdStatus.PendingModeration;
        ad.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var result = new PublicationResult { Ok = true };

        // 6) Публикация и/или модерация по каждому каналу
        var publishedLinks = new List<string>();
        var pending = 0;

        foreach (var channel in channels)
        {
            // Проверка подписки на конкретный канал (если включено)
            if (channel.RequireSubscription)
            {
                var subChannel = channel.SubscriptionChannelUsername
                                 ?? channel.TelegramUsername
                                 ?? globalSub;

                if (!string.IsNullOrWhiteSpace(subChannel))
                {
                    var okSub = await _subscriptionService.IsSubscribedAsync(telegramUserId, subChannel);
                    if (!okSub)
                    {
                        _logger.LogInformation("Пользователь {UserId} не подписан на {Channel}, публикация в этот канал пропущена.", telegramUserId, subChannel);
                        continue;
                    }
                }
            }

            // Доп. анти-спам фильтр по настройкам канала
            if (channel.EnableSpamFilter)
            {
                var shouldCheck = channel.SpamFilterFreeOnly ? !ad.IsPaid : true;
                if (shouldCheck)
                {
                    if (_linkGuard.ContainsForbiddenLinks(ad.Title) ||
                        _linkGuard.ContainsForbiddenLinks(ad.Text) ||
                        _linkGuard.ContainsForbiddenLinks(ad.Contacts))
                    {
                        _logger.LogInformation("Анти-спам: объявление {AdId} не прошло фильтр для канала {ChannelId}.", ad.Id, channel.Id);
                        continue;
                    }
                }
            }

            if (channel.ModerationMode == ChannelModerationMode.Moderation)
            {
                await _moderationService.CreateRequestAsync(ad.Id, channel.Id);
                pending++;
                continue;
            }

            var text = _formatter.BuildPostText(ad, cat, channel);
            var (_, link) = await _publisher.PublishAsync(ad, cat, channel, text, isBump: false);
            if (!string.IsNullOrWhiteSpace(link))
                publishedLinks.Add(link);
        }

        // 7) Обновляем статус объявления
        if (publishedLinks.Count > 0)
        {
            ad.Status = Domain.Enums.AdStatus.Published;
        }
        else if (pending > 0)
        {
            ad.Status = Domain.Enums.AdStatus.PendingModeration;
        }
        else
        {
            ad.Status = Domain.Enums.AdStatus.Rejected;
        }

        ad.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        // 8) Увеличиваем суточный счётчик для бесплатных только если объявление действительно принято
        var accepted = publishedLinks.Count > 0 || pending > 0;
        if (!ad.IsPaid && accepted)
            await _dailyLimit.IncrementFreePublishAsync(telegramUserId);

        result.PublishedLinks = publishedLinks;
        result.PendingModerationCount = pending;

        if (publishedLinks.Count > 0 && pending > 0)
        {
            result.Message = $"Опубликовано в {publishedLinks.Count} каналах. Ещё {pending} канал(ов) — на модерации.";
        }
        else if (publishedLinks.Count > 0)
        {
            result.Message = $"Опубликовано в {publishedLinks.Count} каналах.";
        }
        else if (pending > 0)
        {
            result.Message = $"Объявление отправлено на модерацию в {pending} канал(ов). Ссылка придёт после одобрения.";
        }
        else
        {
            result.Ok = false;
            result.Message = "Не удалось опубликовать: объявление не прошло фильтры/проверки ни для одного канала.";
        }

        return result;
    }

    /// <summary>
    /// Платное поднятие объявления (bump): публикует объявление заново во все каналы категории.
    /// </summary>
    public async Task<List<string>> BumpAsync(long telegramUserId, Guid adId)
    {
        var user = await _userService.GetByTelegramIdAsync(telegramUserId);
        if (user == null)
            return new List<string>();

        var ad = await _db.Ads.FirstOrDefaultAsync(x => x.Id == adId && x.UserId == user.Id);
        if (ad == null)
            return new List<string>();

        var cat = await _categoryService.GetByIdAsync(ad.CategoryId);
        if (cat == null)
            return new List<string>();

        var channels = await _channelService.GetChannelsForCategoryAsync(ad.CategoryId);
        var links = new List<string>();

        foreach (var channel in channels.Where(x => x.IsActive))
        {
            // bump публикуем только в auto-каналы
            if (channel.ModerationMode != ChannelModerationMode.Auto)
                continue;

            var text = _formatter.BuildPostText(ad, cat, channel);
            var (_, link) = await _publisher.PublishAsync(ad, cat, channel, text, isBump: true);
            if (!string.IsNullOrWhiteSpace(link))
                links.Add(link);
        }

        ad.BumpCount += 1;
        ad.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return links;
    }
}
