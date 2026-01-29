namespace SewingAdsBot.Api.Domain.Enums;

/// <summary>
/// Статус объявления.
/// </summary>
public enum AdStatus
{
    /// <summary>
    /// Черновик (пользователь ещё вводит данные).
    /// </summary>
    Draft = 0,

    /// <summary>
    /// Ожидает модерации (хотя бы в одном канале).
    /// </summary>
    PendingModeration = 1,

    /// <summary>
    /// Опубликовано (хотя бы в одном канале).
    /// </summary>
    Published = 2,

    /// <summary>
    /// Отклонено (все заявки отклонены).
    /// </summary>
    Rejected = 3
}
