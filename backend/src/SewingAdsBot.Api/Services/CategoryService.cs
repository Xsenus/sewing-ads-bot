using Microsoft.EntityFrameworkCore;
using SewingAdsBot.Api.Data;
using SewingAdsBot.Api.Domain.Entities;
using SewingAdsBot.Api.Utilities;

namespace SewingAdsBot.Api.Services;

/// <summary>
/// Сервис категорий.
/// </summary>
public sealed class CategoryService
{
    private readonly AppDbContext _db;

    /// <summary>
    /// Конструктор.
/// </summary>
    public CategoryService(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Получить категорию по ID.
/// </summary>
    public Task<Category?> GetByIdAsync(Guid id)
        => _db.Categories.FirstOrDefaultAsync(x => x.Id == id && x.IsActive);

    /// <summary>
    /// Получить корневые категории.
/// </summary>
    public Task<List<Category>> GetRootAsync()
        => _db.Categories
            .Where(x => x.ParentId == null && x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToListAsync();

    /// <summary>
    /// Получить дочерние категории.
/// </summary>
    public Task<List<Category>> GetChildrenAsync(Guid parentId)
        => _db.Categories
            .Where(x => x.ParentId == parentId && x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToListAsync();

    /// <summary>
    /// Проверить, есть ли у категории дети.
/// </summary>
    public Task<bool> HasChildrenAsync(Guid categoryId)
        => _db.Categories.AnyAsync(x => x.ParentId == categoryId && x.IsActive);

    /// <summary>
    /// Создать категорию, если она ещё не существует (по Name + ParentId).
    /// Используется для seed/админки.
/// </summary>
    public async Task<Category> EnsureAsync(string name, Guid? parentId, int sortOrder = 0)
    {
        var existing = await _db.Categories.FirstOrDefaultAsync(x =>
            x.Name == name && x.ParentId == parentId);

        if (existing != null)
        {
            existing.IsActive = true;
            existing.SortOrder = sortOrder;
            if (string.IsNullOrWhiteSpace(existing.Slug))
                existing.Slug = TextSlug.ToSlug(name);

            await _db.SaveChangesAsync();
            return existing;
        }

        var cat = new Category
        {
            Name = name,
            ParentId = parentId,
            SortOrder = sortOrder,
            Slug = TextSlug.ToSlug(name),
            IsActive = true
        };

        _db.Categories.Add(cat);
        await _db.SaveChangesAsync();
        return cat;
    }

    /// <summary>
    /// Обновить категорию.
/// </summary>
    public async Task UpdateAsync(Category category)
    {
        category.Slug = string.IsNullOrWhiteSpace(category.Slug)
            ? TextSlug.ToSlug(category.Name)
            : TextSlug.ToSlug(category.Slug);

        _db.Categories.Update(category);
        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Удалить (деактивировать) категорию.
/// </summary>
    public async Task DeactivateAsync(Guid id)
    {
        var cat = await _db.Categories.FirstOrDefaultAsync(x => x.Id == id);
        if (cat == null) return;

        cat.IsActive = false;
        await _db.SaveChangesAsync();
    }
}
