using System.Net;

namespace SewingAdsBot.Api.Utilities;

/// <summary>
/// Утилиты для безопасной отправки текста в Telegram с ParseMode=HTML.
/// </summary>
public static class HtmlUtil
{
    /// <summary>
    /// Экранировать текст под HTML.
/// </summary>
    public static string Escape(string? input)
        => WebUtility.HtmlEncode(input ?? string.Empty);
}
