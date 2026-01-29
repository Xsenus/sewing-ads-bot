using System.Text;
using System.Text.RegularExpressions;

namespace SewingAdsBot.Api.Utilities;

/// <summary>
/// Утилиты для формирования slug и хештегов.
/// </summary>
public static class TextSlug
{
    private static readonly Regex NonWordRegex = new(@"[^\p{L}\p{Nd}]+", RegexOptions.Compiled);

    /// <summary>
    /// Превратить произвольную строку в slug для хештега:
    /// пробелы/знаки → "_", множественные "_" схлопываются.
    /// </summary>
    public static string ToSlug(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var s = input.Trim();

        // Меняем любые не-буквы/цифры на "_"
        s = NonWordRegex.Replace(s, "_");

        // Схлопываем множественные "_"
        s = Regex.Replace(s, "_{2,}", "_");

        s = s.Trim('_');

        // Telegram хештеги могут быть кириллицей — не транслитерируем.
        return s;
    }

    /// <summary>
    /// Сформировать хештег из текста.
    /// </summary>
    public static string ToHashtag(string input)
    {
        var slug = ToSlug(input);
        if (string.IsNullOrWhiteSpace(slug))
            return string.Empty;

        return "#" + slug;
    }
}
