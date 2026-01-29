using System.Text.RegularExpressions;

namespace SewingAdsBot.Api.Utilities;

/// <summary>
/// Валидация контактов: разрешаем только Telegram ЛС (username), телефон, email.
/// </summary>
public static class ContactsValidator
{
    private static readonly Regex TelegramUsernameRegex = new(@"^@?[a-zA-Z0-9_]{5,32}$", RegexOptions.Compiled);
    private static readonly Regex PhoneRegex = new(@"^\+?[0-9][0-9\-\s]{7,18}$", RegexOptions.Compiled);
    private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

    /// <summary>
    /// Проверить строку контактов. Можно несколько строк/контактов через запятую.
/// </summary>
    public static bool IsValid(string contacts)
    {
        if (string.IsNullOrWhiteSpace(contacts))
            return false;

        var parts = contacts
            .Split(new[] { '\n', '\r', ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length == 0)
            return false;

        foreach (var part in parts)
        {
            // Запрещаем ссылки t.me и подобные: контакт должен быть @username, а не URL.
            if (part.Contains("t.me", StringComparison.OrdinalIgnoreCase) ||
                part.Contains("telegram.me", StringComparison.OrdinalIgnoreCase) ||
                part.Contains("http", StringComparison.OrdinalIgnoreCase))
                return false;

            if (TelegramUsernameRegex.IsMatch(part))
                continue;

            if (PhoneRegex.IsMatch(part))
                continue;

            if (EmailRegex.IsMatch(part))
                continue;

            return false;
        }

        return true;
    }
}
