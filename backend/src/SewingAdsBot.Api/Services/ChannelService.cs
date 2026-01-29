using Microsoft.EntityFrameworkCore;
using SewingAdsBot.Api.Data;
using SewingAdsBot.Api.Domain.Entities;

namespace SewingAdsBot.Api.Services;

/// <summary>
/// Сервис каналов и связей категория→канал.
/// </summary>
public sealed class ChannelService
{
    private readonly AppDbContext _db;

    /// <summary>
    /// Конструктор.
/// </summary>
    public ChannelService(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Получить все каналы (включая неактивные).
    /// </summary>
    public Task<List<Channel>> GetAllAsync()
        => _db.Channels.OrderBy(x => x.Title).ToListAsync();

    /// <summary>
    /// Получить только активные каналы.
    /// </summary>
    public Task<List<Channel>> GetActiveAsync()
        => _db.Channels.Where(x => x.IsActive).OrderBy(x => x.Title).ToListAsync();

    /// <summary>
    /// Получить канал по ID.
    /// </summary>
    public Task<Channel?> GetByIdAsync(Guid id)
        => _db.Channels.FirstOrDefaultAsync(x => x.Id == id);

    /// <summary>
    /// Создать канал, если ещё нет по TelegramChatId.
    /// </summary>
    public async Task<Channel> EnsureAsync(Channel channel)
    {
        var existing = await _db.Channels.FirstOrDefaultAsync(x => x.TelegramChatId == channel.TelegramChatId);
        if (existing != null)
        {
            existing.Title = channel.Title;
            existing.TelegramUsername = channel.TelegramUsername;
            existing.IsActive = channel.IsActive;
            existing.ModerationMode = channel.ModerationMode;
            existing.EnableSpamFilter = channel.EnableSpamFilter;
            existing.SpamFilterFreeOnly = channel.SpamFilterFreeOnly;
            existing.RequireSubscription = channel.RequireSubscription;
            existing.SubscriptionChannelUsername = channel.SubscriptionChannelUsername;
            existing.FooterLinkText = channel.FooterLinkText;
            existing.FooterLinkUrl = channel.FooterLinkUrl;

            await _db.SaveChangesAsync();
            return existing;
        }

        _db.Channels.Add(channel);
        await _db.SaveChangesAsync();
        return channel;
    }

    /// <summary>
    /// Обновить канал.
    /// </summary>
    public async Task UpdateAsync(Channel channel)
    {
        _db.Channels.Update(channel);
        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Получить каналы, настроенные для выбранной категории (вариант C).
    /// Если прямых связей нет — делаем fallback к родителю и так до корня.
    /// </summary>
    public async Task<List<Channel>> GetChannelsForCategoryAsync(Guid categoryId)
    {
        // 1) Прямые маппинги
        var direct = await (from cc in _db.CategoryChannels
                            join ch in _db.Channels on cc.ChannelId equals ch.Id
                            where cc.CategoryId == categoryId
                                  && cc.IsEnabled
                                  && ch.IsActive
                            select ch).ToListAsync();

        if (direct.Count > 0)
            return direct.OrderBy(x => x.Title).ToList();

        // 2) Fallback к родителю
        var parentId = await _db.Categories.Where(x => x.Id == categoryId).Select(x => x.ParentId).FirstOrDefaultAsync();
        if (parentId == null)
            return new List<Channel>();

        return await GetChannelsForCategoryAsync(parentId.Value);
    }

    /// <summary>
    /// Установить список каналов для категории (перезаписать связи).
    /// </summary>
    public async Task SetCategoryChannelsAsync(Guid categoryId, IEnumerable<Guid> channelIds)
    {
        var existing = await _db.CategoryChannels.Where(x => x.CategoryId == categoryId).ToListAsync();
        _db.CategoryChannels.RemoveRange(existing);

        foreach (var chId in channelIds.Distinct())
        {
            _db.CategoryChannels.Add(new CategoryChannel
            {
                CategoryId = categoryId,
                ChannelId = chId,
                IsEnabled = true
            });
        }

        await _db.SaveChangesAsync();
    }
}
