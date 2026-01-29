using Microsoft.EntityFrameworkCore;
using SewingAdsBot.Api.Data;
using SewingAdsBot.Api.Domain.Entities;

namespace SewingAdsBot.Api.Services;

/// <summary>
/// Поиск опубликованных объявлений.
/// </summary>
public sealed class SearchService
{
    private readonly AppDbContext _db;

    /// <summary>
    /// Конструктор.
/// </summary>
    public SearchService(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Найти опубликованные объявления по категории (включая подкатегории) и ключевым словам.
    /// При наличии страны/города — фильтруем по ним.
/// </summary>
    public async Task<List<Ad>> SearchAsync(Guid categoryId, string? keywords, string? country, string? city, int take = 5)
    {
        var categoryIds = await GetDescendantCategoryIdsAsync(categoryId);

        var q = _db.Ads.AsQueryable();

        q = q.Where(x => x.Status == Domain.Enums.AdStatus.Published && categoryIds.Contains(x.CategoryId));

        if (!string.IsNullOrWhiteSpace(country))
            q = q.Where(x => x.Country == country);

        if (!string.IsNullOrWhiteSpace(city))
            q = q.Where(x => x.City == city);

        if (!string.IsNullOrWhiteSpace(keywords))
        {
            var kw = keywords.Trim();
            q = q.Where(x => x.Title.Contains(kw) || x.Text.Contains(kw));
        }

        return await q.OrderByDescending(x => x.CreatedAtUtc)
            .Take(take)
            .ToListAsync();
    }

    private async Task<List<Guid>> GetDescendantCategoryIdsAsync(Guid rootId)
    {
        var result = new List<Guid> { rootId };
        var queue = new Queue<Guid>();
        queue.Enqueue(rootId);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            var children = await _db.Categories.Where(x => x.ParentId == current && x.IsActive).Select(x => x.Id).ToListAsync();
            foreach (var ch in children)
            {
                if (result.AddUnique(ch))
                    queue.Enqueue(ch);
            }
        }

        return result;
    }
}

/// <summary>
/// Небольшой helper для списка.
/// </summary>
internal static class ListExtensions
{
    /// <summary>
    /// Добавить элемент в список если его там ещё нет.
/// </summary>
    public static bool AddUnique<T>(this List<T> list, T item)
    {
        if (list.Contains(item)) return false;
        list.Add(item);
        return true;
    }
}
