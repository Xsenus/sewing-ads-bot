using System.ComponentModel.DataAnnotations;
using SewingAdsBot.Api.Domain.Enums;

namespace SewingAdsBot.Api.Domain.Entities;

/// <summary>
/// Канал, куда бот может публиковать объявления.
/// </summary>
public sealed class Channel
{
    /// <summary>
    /// Внутренний идентификатор.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Название (для админки).
    /// </summary>
    [MaxLength(128)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Telegram Chat ID канала (например -100123...).
    /// </summary>
    public long TelegramChatId { get; set; }

    /// <summary>
    /// Username канала (без @), опционально.
    /// </summary>
    [MaxLength(64)]
    public string? TelegramUsername { get; set; }

    /// <summary>
    /// Активен ли канал (если false — публикации в него не выполняются).
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Режим публикации.
    /// </summary>
    public ChannelModerationMode ModerationMode { get; set; } = ChannelModerationMode.Auto;

    /// <summary>
    /// Включить анти-спам фильтр (ссылки).
    /// </summary>
    public bool EnableSpamFilter { get; set; } = true;

    /// <summary>
    /// Применять анти-спам фильтр только к бесплатным объявлениям.
    /// </summary>
    public bool SpamFilterFreeOnly { get; set; } = true;

    /// <summary>
    /// Требовать подписку на канал перед публикацией в этот канал.
    /// </summary>
    public bool RequireSubscription { get; set; } = true;

    /// <summary>
    /// Username канала для проверки подписки (без @). Если пусто — используется TelegramUsername.
    /// </summary>
    [MaxLength(64)]
    public string? SubscriptionChannelUsername { get; set; }

    /// <summary>
    /// Текст системной ссылки, добавляемой в конце поста.
    /// </summary>
    [MaxLength(128)]
    public string FooterLinkText { get; set; } = "Швейные производства • Объявления";

    /// <summary>
    /// URL системной ссылки, добавляемой в конце поста.
    /// </summary>
    [MaxLength(256)]
    public string FooterLinkUrl { get; set; } = "https://t.me/sewing_industries";

    /// <summary>
    /// ID закреплённого сообщения (если бот закреплял «ОПУБЛИКОВАТЬ»).
    /// </summary>
    public int? PinnedMessageId { get; set; }
}
