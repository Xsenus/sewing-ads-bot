using SewingAdsBot.Api.Domain.Entities;
using SewingAdsBot.Api.Domain.Enums;
using SewingAdsBot.Api.Services;

namespace SewingAdsBot.Api.Seed;

/// <summary>
/// Начальный seed данных:
/// - создаёт первого админа админки
/// - создаёт категории из ТЗ
/// - создаёт пример канала и базовые настройки
/// </summary>
public sealed class DatabaseSeeder
{
    private readonly CategoryService _categories;
    private readonly ChannelService _channels;
    private readonly AdminAuthService _adminAuth;

    /// <summary>
    /// Конструктор.
/// </summary>
    public DatabaseSeeder(CategoryService categories, ChannelService channels, AdminAuthService adminAuth)
    {
        _categories = categories;
        _channels = channels;
        _adminAuth = adminAuth;
    }

    /// <summary>
    /// Выполнить seed.
/// </summary>
    public async Task SeedAsync()
    {
        await _adminAuth.EnsureInitialAdminAsync();

        // Категории (вытащены из схемы в docx).
        var sewing = await _categories.EnsureAsync("Швейные производства", parentId: null, sortOrder: 10);
        await _categories.EnsureAsync("Ищу швейные производства", parentId: sewing.Id, sortOrder: 10);

        await _categories.EnsureAsync("DTF печать на ткани", parentId: null, sortOrder: 20);
        await _categories.EnsureAsync("Дизайнеры, модельеры", parentId: null, sortOrder: 30);
        await _categories.EnsureAsync("Бирки, лейбы", parentId: null, sortOrder: 40);
        await _categories.EnsureAsync("Ткани", parentId: null, sortOrder: 50);
        await _categories.EnsureAsync("Честный знак, сертификация", parentId: null, sortOrder: 60);
        await _categories.EnsureAsync("Фурнитура", parentId: null, sortOrder: 70);
        await _categories.EnsureAsync("Байеры", parentId: null, sortOrder: 80);
        await _categories.EnsureAsync("Карго перевозки", parentId: null, sortOrder: 90);
        await _categories.EnsureAsync("Скупка", parentId: null, sortOrder: 100);
        await _categories.EnsureAsync("Продажа, аренда", parentId: null, sortOrder: 110);
        await _categories.EnsureAsync("Вакансии", parentId: null, sortOrder: 120);
        await _categories.EnsureAsync("ЗИП пакеты", parentId: null, sortOrder: 130);

        // Пример канала (замените TelegramChatId на реальный -100... и username).
        // Канал по умолчанию выключен, чтобы не было случайных публикаций.
        await _channels.EnsureAsync(new Channel
        {
            Title = "sewing_industries (пример)",
            TelegramChatId = 0,
            TelegramUsername = "sewing_industries",
            IsActive = false,
            ModerationMode = ChannelModerationMode.Auto,
            EnableSpamFilter = true,
            SpamFilterFreeOnly = true,
            RequireSubscription = true,
            SubscriptionChannelUsername = "sewing_industries",
            FooterLinkText = "Швейные производства • Объявления",
            FooterLinkUrl = "https://t.me/sewing_industries"
        });
    }
}
