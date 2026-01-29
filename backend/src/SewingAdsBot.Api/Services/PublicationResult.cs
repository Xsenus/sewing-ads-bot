namespace SewingAdsBot.Api.Services;

/// <summary>
/// Результат попытки публикации объявления.
/// </summary>
public sealed class PublicationResult
{
    /// <summary>
    /// Успешно ли принято объявление (публикация и/или модерация).
    /// </summary>
    public bool Ok { get; set; }

    /// <summary>
    /// Сообщение для пользователя (ошибка или статус).
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Ссылки на опубликованные сообщения (если опубликовано сразу).
    /// </summary>
    public List<string> PublishedLinks { get; set; } = new();

    /// <summary>
    /// Сколько каналов ушло на модерацию.
    /// </summary>
    public int PendingModerationCount { get; set; }
}
