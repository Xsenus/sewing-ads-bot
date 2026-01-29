using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SewingAdsBot.Api.Data;
using SewingAdsBot.Api.Domain.Entities;
using SewingAdsBot.Api.Services;

namespace SewingAdsBot.Api.Controllers;

/// <summary>
/// Управление категориями и связями категория→канал.
/// </summary>
[ApiController]
[Authorize]
[Route("api/admin/categories")]
public sealed class CategoriesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly CategoryService _categories;
    private readonly ChannelService _channels;

    /// <summary>
    /// Конструктор.
/// </summary>
    public CategoriesController(AppDbContext db, CategoryService categories, ChannelService channels)
    {
        _db = db;
        _categories = categories;
        _channels = channels;
    }

    /// <summary>
    /// Получить все категории.
/// </summary>
    [HttpGet]
    public async Task<ActionResult<List<CategoryDto>>> GetAll()
    {
        var list = await _db.Categories.OrderBy(x => x.SortOrder).ThenBy(x => x.Name).ToListAsync();
        return Ok(list.Select(ToDto).ToList());
    }

    /// <summary>
    /// Создать категорию.
/// </summary>
    [HttpPost]
    public async Task<ActionResult<CategoryDto>> Create([FromBody] CreateCategoryRequest req)
    {
        var cat = await _categories.EnsureAsync(req.Name, req.ParentId, req.SortOrder);
        cat.IsActive = req.IsActive;
        await _categories.UpdateAsync(cat);

        return Ok(ToDto(cat));
    }

    /// <summary>
    /// Обновить категорию.
/// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CategoryDto>> Update(Guid id, [FromBody] UpdateCategoryRequest req)
    {
        var cat = await _db.Categories.FirstOrDefaultAsync(x => x.Id == id);
        if (cat == null) return NotFound();

        cat.Name = req.Name;
        cat.Slug = req.Slug;
        cat.ParentId = req.ParentId;
        cat.SortOrder = req.SortOrder;
        cat.IsActive = req.IsActive;

        await _categories.UpdateAsync(cat);

        return Ok(ToDto(cat));
    }

    /// <summary>
    /// Деактивировать категорию.
/// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        await _categories.DeactivateAsync(id);
        return NoContent();
    }

    /// <summary>
    /// Установить каналы для категории (вариант C).
    /// </summary>
    [HttpPut("{id:guid}/channels")]
    public async Task<ActionResult> SetCategoryChannels(Guid id, [FromBody] SetCategoryChannelsRequest req)
    {
        await _channels.SetCategoryChannelsAsync(id, req.ChannelIds);
        return Ok();
    }

    /// <summary>
    /// Получить список каналов, назначенных категории (вариант C).
    /// Удобно для админки, чтобы показать текущие выбранные каналы.
    /// </summary>
    [HttpGet("{id:guid}/channels")]
    public async Task<ActionResult<List<Guid>>> GetCategoryChannels(Guid id)
    {
        var ids = await _db.CategoryChannels
            .Where(x => x.CategoryId == id && x.IsEnabled)
            .Select(x => x.ChannelId)
            .ToListAsync();

        return Ok(ids);
    }

    /// <summary>
    /// DTO категории.
/// </summary>
    public sealed class CategoryDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public Guid? ParentId { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// Запрос создания категории.
/// </summary>
    public sealed class CreateCategoryRequest
    {
        public string Name { get; set; } = string.Empty;
        public Guid? ParentId { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// Запрос обновления категории.
/// </summary>
    public sealed class UpdateCategoryRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public Guid? ParentId { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// Запрос на установку связей категория→канал.
/// </summary>
    public sealed class SetCategoryChannelsRequest
    {
        public List<Guid> ChannelIds { get; set; } = new();
    }

    private static CategoryDto ToDto(Category c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        Slug = c.Slug,
        ParentId = c.ParentId,
        SortOrder = c.SortOrder,
        IsActive = c.IsActive
    };
}
