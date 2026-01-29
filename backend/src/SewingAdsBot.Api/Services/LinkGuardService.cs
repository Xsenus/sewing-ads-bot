using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace SewingAdsBot.Api.Services;

/// <summary>
/// Анти-обход фильтр ссылок.
/// Отлавливает как обычные URL, так и обфускации вида:
///  - t . me
///  - t[dot]me
///  - t-me
///  - кириллица (т.ме)
///  - нули вместо букв и т.п.
/// </summary>
public sealed class LinkGuardService
{
    private static readonly Regex UrlLikeRegex = new(
        @"(?ix)\b(
            (https?:\/\/)?(
                (t(?:elegram)?\.?me|tg|tme|t\.me|telegra\.ph|telegram\.dog)|
                (wa\.me|whatsapp)|
                (vk\.com|vkontakte)|
                (instagram|instagr\.am)|
                (facebook|fb\.com)|
                (youtube|youtu\.be)|
                (tiktok)|
                (twitter|x\.com)|
                (discord\.gg|discord)|
                (google\.com\/sheets|docs\.google\.com\/spreadsheets|sheets\.google\.com)
            )
            [^\s]*)
        )",
        RegexOptions.Compiled);

    private static readonly string[] ForbiddenDomainHints =
    {
        "t.me",
        "telegram.me",
        "telegram.dog",
        "telegra.ph",
        "vk.com",
        "vkontakte",
        "instagram",
        "instagr.am",
        "facebook",
        "fb.com",
        "youtube",
        "youtu.be",
        "tiktok",
        "twitter",
        "x.com",
        "discord.gg",
        "discord",
        "wa.me",
        "whatsapp",
        "google.com/sheets",
        "docs.google.com/spreadsheets"
    };

    /// <summary>
    /// Проверить, содержит ли текст запрещённые ссылки/упоминания сервисов.
    /// </summary>
    public bool ContainsForbiddenLinks(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        // Быстрый путь: обычные URL.
        if (UrlLikeRegex.IsMatch(text))
            return true;

        // Медленный путь: нормализуем строку и ищем признаки доменов/ссылок.
        var normalized = Normalize(text);

        // Признаки "http", "www", "/" после домена и т.п.
        if (normalized.Contains("http://") || normalized.Contains("https://") || normalized.Contains("www."))
            return true;

        foreach (var hint in ForbiddenDomainHints)
        {
            var h = Normalize(hint);
            if (normalized.Contains(h))
                return true;
        }

        // Общий доменный паттерн: something.tld
        // С учётом того, что пробелы удалены, шанс ложных срабатываний минимален.
        if (Regex.IsMatch(normalized, @"(?i)[a-z0-9\-]{2,}\.(ru|com|net|org|io|app|site|dev|pro|me|xyz|shop|store|info|gg)\b"))
            return true;

        return false;
    }

    /// <summary>
    /// Нормализация текста для анти-обхода: lower, удаление пробелов/скобок/мусора,
    /// замена [dot] на ".", кириллица → латиница-аналоги, 0→o и т.п.
    /// </summary>
    private static string Normalize(string input)
    {
        var lower = input.ToLowerInvariant();

        // Нормализуем Юникод (убираем составные символы).
        lower = lower.Normalize(NormalizationForm.FormKC);

        // Типовые замены "dot"
        lower = lower.Replace("[dot]", ".", StringComparison.OrdinalIgnoreCase)
            .Replace("(dot)", ".", StringComparison.OrdinalIgnoreCase)
            .Replace("{dot}", ".", StringComparison.OrdinalIgnoreCase)
            .Replace(" dot ", ".", StringComparison.OrdinalIgnoreCase);

        // Убираем пробелы и большинство разделителей, чтобы ловить "t . me"
        var sb = new StringBuilder(lower.Length);
        foreach (var ch in lower)
        {
            if (char.IsWhiteSpace(ch))
                continue;

            // Убираем "мусорные" символы
            if (ch is '[' or ']' or '(' or ')' or '{' or '}' or '<' or '>' or '"' or '\'' or '`')
                continue;

            sb.Append(MapLookalike(ch));
        }

        // Дополнительные замены цифр/букв
        var s = sb.ToString();
        s = s.Replace("0", "o", StringComparison.Ordinal)
             .Replace("1", "l", StringComparison.Ordinal);

        return s;
    }

    /// <summary>
    /// Замена кириллицы и визуально похожих символов на латиницу.
    /// </summary>
    private static char MapLookalike(char ch)
    {
        return ch switch
        {
            // Кириллица -> латиница
            'а' => 'a',
            'е' => 'e',
            'о' => 'o',
            'р' => 'p',
            'с' => 'c',
            'у' => 'y',
            'х' => 'x',
            'к' => 'k',
            'м' => 'm',
            'т' => 't',
            'н' => 'h',
            'в' => 'b',
            'і' => 'i',
            'ё' => 'e',
            'й' => 'i',
            // "точка" и похожие
            '·' => '.',
            '•' => '.',
            '。' => '.',
            '､' => '.',
            // Разделители -> ничего
            '–' => '-',
            '—' => '-',
            _ => ch
        };
    }
}
