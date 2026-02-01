using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SewingAdsBot.Api.Data;
using SewingAdsBot.Api.Domain.Entities;
using SewingAdsBot.Api.Domain.Enums;
using SewingAdsBot.Api.Options;
using SewingAdsBot.Api.Utilities;

namespace SewingAdsBot.Api.Services;

/// <summary>
/// Сервис для создания/редактирования объявлений.
/// </summary>
public sealed class AdService
{
    private readonly AppDbContext _db;
    private readonly SettingsService _settings;
    private readonly LimitOptions _limits;

    /// <summary>
    /// Конструктор.
/// </summary>
    public AdService(AppDbContext db, SettingsService settings, IOptions<LimitOptions> limits)
    {
        _db = db;
        _settings = settings;
        _limits = limits.Value;
    }

    /// <summary>
    /// Создать черновик объявления.
/// </summary>
    public async Task<Ad> CreateDraftAsync(User user, Guid categoryId, bool isPaid)
    {
        if (string.IsNullOrWhiteSpace(user.Country) || string.IsNullOrWhiteSpace(user.City))
            throw new InvalidOperationException("Сначала укажите страну и город в профиле.");

        if (!isPaid)
        {
            var allowFree = await _settings.GetBoolAsync("Ads.EnableFreeAds", defaultValue: true);
            if (!allowFree)
                throw new InvalidOperationException("Бесплатные объявления отключены администратором.");
        }

        var ad = new Ad
        {
            UserId = user.Id,
            CategoryId = categoryId,
            IsPaid = isPaid,
            Country = user.Country!,
            City = user.City!,
            Status = AdStatus.Draft
        };

        _db.Ads.Add(ad);
        await _db.SaveChangesAsync();
        return ad;
    }

    /// <summary>
    /// Получить объявление по ID.
/// </summary>
    public Task<Ad?> GetByIdAsync(Guid adId)
        => _db.Ads.FirstOrDefaultAsync(x => x.Id == adId);

    /// <summary>
    /// Получить список объявлений пользователя.
/// </summary>
    public Task<List<Ad>> GetUserAdsAsync(Guid userId, int take = 20)
        => _db.Ads.Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(take)
            .ToListAsync();

    /// <summary>
    /// Обновить заголовок.
/// </summary>
    public async Task<(bool ok, string? error)> SetTitleAsync(Guid adId, string title)
    {
        title = title.Trim();
        if (title.Length == 0)
            return (false, "Заголовок не может быть пустым.");

        var titleMax = Math.Min(await _settings.GetIntAsync("Limits.TitleMax", _limits.TitleMax), 200);
        if (title.Length > titleMax)
            return (false, $"Заголовок слишком длинный. Макс. {titleMax} символов.");

        var ad = await _db.Ads.FirstOrDefaultAsync(x => x.Id == adId);
        if (ad == null) return (false, "Объявление не найдено.");

        ad.Title = title;
        ad.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return (true, null);
    }

    /// <summary>
    /// Обновить текст объявления.
/// </summary>
    public async Task<(bool ok, string? error)> SetTextAsync(Guid adId, string text)
    {
        text = text.Trim();
        if (text.Length == 0)
            return (false, "Текст не может быть пустым.");

        var textMax = Math.Min(await _settings.GetIntAsync("Limits.TextMax", _limits.TextMax), 2000);
        if (text.Length > textMax)
            return (false, $"Текст слишком длинный. Макс. {textMax} символов.");

        var ad = await _db.Ads.FirstOrDefaultAsync(x => x.Id == adId);
        if (ad == null) return (false, "Объявление не найдено.");

        ad.Text = text;
        ad.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return (true, null);
    }

    /// <summary>
    /// Обновить контакты.
/// </summary>
    public async Task<(bool ok, string? error)> SetContactsAsync(Guid adId, string contacts)
    {
        contacts = contacts.Trim();
        if (contacts.Length == 0)
            return (false, "Контакты не могут быть пустыми.");

        var contactsMax = Math.Min(await _settings.GetIntAsync("Limits.ContactsMax", _limits.ContactsMax), 256);
        if (contacts.Length > contactsMax)
            return (false, $"Контакты слишком длинные. Макс. {contactsMax} символов.");

        if (!ContactsValidator.IsValid(contacts))
            return (false, "Контакты должны быть только: Telegram ЛС (@username), телефон или Email.");

        var ad = await _db.Ads.FirstOrDefaultAsync(x => x.Id == adId);
        if (ad == null) return (false, "Объявление не найдено.");

        ad.Contacts = contacts;
        ad.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return (true, null);
    }

    /// <summary>
    /// Установить медиа (только для платного объявления).
    /// </summary>
    public async Task<(bool ok, string? error)> SetMediaAsync(Guid adId, MediaType mediaType, string fileId)
    {
        var ad = await _db.Ads.FirstOrDefaultAsync(x => x.Id == adId);
        if (ad == null) return (false, "Объявление не найдено.");

        if (!ad.IsPaid)
        {
            var allowMediaInFree = await _settings.GetBoolAsync("Ads.FreeAllowMedia", defaultValue: false);
            if (!allowMediaInFree)
                return (false, "В бесплатном объявлении нельзя добавлять фото/видео. Выберите платное.");
        }

        ad.MediaType = mediaType;
        ad.MediaFileId = fileId;
        ad.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return (true, null);
    }

    /// <summary>
    /// Проверить, что объявление заполнено и готово к отправке на публикацию.
/// </summary>
    public async Task<(bool ok, string? error)> ValidateReadyAsync(Guid adId)
    {
        var ad = await _db.Ads.FirstOrDefaultAsync(x => x.Id == adId);
        if (ad == null) return (false, "Объявление не найдено.");

        if (string.IsNullOrWhiteSpace(ad.Title))
            return (false, "Не указан заголовок.");

        if (string.IsNullOrWhiteSpace(ad.Text))
            return (false, "Не указан текст.");

        if (string.IsNullOrWhiteSpace(ad.Contacts))
            return (false, "Не указаны контакты.");

        return (true, null);
    }

    /// <summary>
    /// Перевести объявление из Draft в PendingModeration (или Published будет выставлен позже PublicationService).
    /// </summary>
    public async Task MarkSubmittedAsync(Guid adId)
    {
        var ad = await _db.Ads.FirstOrDefaultAsync(x => x.Id == adId);
        if (ad == null) return;

        ad.Status = AdStatus.PendingModeration;
        ad.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Обновить категорию для объявления (используется при редактировании).
    /// </summary>
    public async Task UpdateCategoryAsync(Guid adId, Guid categoryId)
    {
        var ad = await _db.Ads.FirstOrDefaultAsync(x => x.Id == adId);
        if (ad == null) return;

        ad.CategoryId = categoryId;
        ad.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }
}
