using System.ComponentModel.DataAnnotations;

namespace SewingAdsBot.Api.Domain.Entities;

/// <summary>
/// Категория объявлений (поддерживает древовидную структуру).
/// </summary>
public sealed class Category
{
    /// <summary>
    /// Внутренний идентификатор.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Отображаемое название категории.
    /// </summary>
    [MaxLength(128)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Slug для хештега (например: Ищу_швейное_производство).
    /// </summary>
    [MaxLength(128)]
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// Родительская категория (если null — это корень).
    /// </summary>
    public Guid? ParentId { get; set; }

    /// <summary>
    /// Порядок сортировки в UI.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Активна ли категория.
    /// </summary>
    public bool IsActive { get; set; } = true;
}
