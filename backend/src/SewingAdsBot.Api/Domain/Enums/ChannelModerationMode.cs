namespace SewingAdsBot.Api.Domain.Enums;

/// <summary>
/// Режим публикации в канале.
/// </summary>
public enum ChannelModerationMode
{
    /// <summary>
    /// Публикация без модерации.
/// </summary>
    Auto = 0,

    /// <summary>
    /// Публикация после модерации.
/// </summary>
    Moderation = 1
}
