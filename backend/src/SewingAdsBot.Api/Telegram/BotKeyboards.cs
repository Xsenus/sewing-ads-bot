using SewingAdsBot.Api.Domain.Entities;
using Telegram.Bot.Types.ReplyMarkups;

namespace SewingAdsBot.Api.Telegram;

/// <summary>
/// Генерация клавиатур Telegram.
/// </summary>
public static class BotKeyboards
{
    /// <summary>
    /// Главное меню (ReplyKeyboard).
    /// </summary>
    public static ReplyKeyboardMarkup MainMenu(string language)
        => new(new[]
        {
            new KeyboardButton[] { BotTexts.Text(language, BotTextKeys.MenuCreateAd), BotTexts.Text(language, BotTextKeys.MenuSearchAd) },
            new KeyboardButton[] { BotTexts.Text(language, BotTextKeys.MenuProfile), BotTexts.Text(language, BotTextKeys.MenuHelp) },
            new KeyboardButton[] { BotTexts.Text(language, BotTextKeys.MenuPaidAd) }
        })
        {
            ResizeKeyboard = true
        };

    /// <summary>
    /// Меню профиля (ReplyKeyboard).
    /// </summary>
    public static ReplyKeyboardMarkup ProfileMenu(string language)
        => new(new[]
        {
            new KeyboardButton[] { BotTexts.Text(language, BotTextKeys.MenuLocation), BotTexts.Text(language, BotTextKeys.MenuMyAds) },
            new KeyboardButton[] { BotTexts.Text(language, BotTextKeys.MenuReferral), BotTexts.Text(language, BotTextKeys.MenuBack) }
        })
        {
            ResizeKeyboard = true
        };

    /// <summary>
    /// Клавиатура предпросмотра бесплатного объявления.
/// </summary>
    public static InlineKeyboardMarkup PreviewFree(Guid adId, bool allowMedia, string language)
    {
        var rows = new List<List<InlineKeyboardButton>>
        {
            new()
            {
                InlineKeyboardButton.WithCallbackData(BotTexts.Text(language, BotTextKeys.PreviewPublish), $"create:publish:{adId}")
            },
            new()
            {
                InlineKeyboardButton.WithCallbackData(BotTexts.Text(language, BotTextKeys.PreviewEditTitle), $"create:edit:title:{adId}"),
                InlineKeyboardButton.WithCallbackData(BotTexts.Text(language, BotTextKeys.PreviewEditText), $"create:edit:text:{adId}")
            }
        };

        var editRow = new List<InlineKeyboardButton>
        {
            InlineKeyboardButton.WithCallbackData(BotTexts.Text(language, BotTextKeys.PreviewEditContacts), $"create:edit:contacts:{adId}")
        };

        if (allowMedia)
            editRow.Add(InlineKeyboardButton.WithCallbackData(BotTexts.Text(language, BotTextKeys.PreviewEditMedia), $"create:edit:media:{adId}"));

        rows.Add(editRow);
        rows.Add(new List<InlineKeyboardButton>
        {
            InlineKeyboardButton.WithCallbackData(BotTexts.Text(language, BotTextKeys.PreviewCancel), "create:cancel")
        });

        return new InlineKeyboardMarkup(rows);
    }

    /// <summary>
    /// Клавиатура предпросмотра платного объявления.
    /// </summary>
    public static InlineKeyboardMarkup PreviewPaid(Guid adId, string language)
        => new(new[]
        {
            new []
            {
                InlineKeyboardButton.WithCallbackData(BotTexts.Text(language, BotTextKeys.PreviewPayPublish), $"create:pay:{adId}"),
            },
            new []
            {
                InlineKeyboardButton.WithCallbackData(BotTexts.Text(language, BotTextKeys.PreviewEditTitle), $"create:edit:title:{adId}"),
                InlineKeyboardButton.WithCallbackData(BotTexts.Text(language, BotTextKeys.PreviewEditText), $"create:edit:text:{adId}")
            },
            new []
            {
                InlineKeyboardButton.WithCallbackData(BotTexts.Text(language, BotTextKeys.PreviewEditContacts), $"create:edit:contacts:{adId}"),
                InlineKeyboardButton.WithCallbackData(BotTexts.Text(language, BotTextKeys.PreviewEditMedia), $"create:edit:media:{adId}")
            },
            new []
            {
                InlineKeyboardButton.WithCallbackData(BotTexts.Text(language, BotTextKeys.PreviewCancel), "create:cancel")
            }
        });

    /// <summary>
    /// Клавиатура выбора типа объявления.
    /// </summary>
    public static InlineKeyboardMarkup AdType(string language, bool allowFree, bool allowPaid = true)
    {
        var rows = new List<List<InlineKeyboardButton>>();
        var row = new List<InlineKeyboardButton>();

        if (allowFree)
            row.Add(InlineKeyboardButton.WithCallbackData(BotTexts.Text(language, BotTextKeys.AdTypeFree), "type:free"));

        if (allowPaid)
            row.Add(InlineKeyboardButton.WithCallbackData(BotTexts.Text(language, BotTextKeys.AdTypePaid), "type:paid"));

        if (row.Count > 0)
            rows.Add(row);

        return new InlineKeyboardMarkup(rows);
    }

    /// <summary>
    /// Клавиатура выбора категории (inline).
    /// </summary>
    public static InlineKeyboardMarkup Categories(IEnumerable<Category> categories, string? backCallbackData = null)
    {
        var rows = new List<List<InlineKeyboardButton>>();

        var list = categories.ToList();
        for (int i = 0; i < list.Count; i += 2)
        {
            var row = new List<InlineKeyboardButton>();
            row.Add(InlineKeyboardButton.WithCallbackData(list[i].Name, $"cat:{list[i].Id}"));

            if (i + 1 < list.Count)
                row.Add(InlineKeyboardButton.WithCallbackData(list[i + 1].Name, $"cat:{list[i + 1].Id}"));

            rows.Add(row);
        }

        if (!string.IsNullOrWhiteSpace(backCallbackData))
        {
            rows.Add(new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData("⬅️ Назад", backCallbackData)
            });
        }

        return new InlineKeyboardMarkup(rows);
    }

    /// <summary>
    /// Кнопка "Пропустить" при добавлении медиа.
/// </summary>
    public static ReplyKeyboardMarkup SkipMedia(string language)
        => new(new[]
        {
            new KeyboardButton[] { BotTexts.Text(language, BotTextKeys.Skip) },
            new KeyboardButton[] { BotTexts.Text(language, BotTextKeys.Cancel) }
        })
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = true
        };

    /// <summary>
    /// Клавиатура выбора языка.
    /// </summary>
    public static InlineKeyboardMarkup LanguageSelection()
        => new(new[]
        {
            new []
            {
                InlineKeyboardButton.WithCallbackData(BotTexts.Text(BotTexts.Ru, BotTextKeys.LanguageRu), "lang:ru"),
                InlineKeyboardButton.WithCallbackData(BotTexts.Text(BotTexts.En, BotTextKeys.LanguageEn), "lang:en")
            }
        });

    /// <summary>
    /// Клавиатура выбора локации.
    /// </summary>
    public static ReplyKeyboardMarkup LocationOptions(IEnumerable<string> options, string language)
    {
        var rows = new List<KeyboardButton[]>();
        var list = options.ToList();
        for (int i = 0; i < list.Count; i += 2)
        {
            if (i + 1 < list.Count)
                rows.Add(new[] { new KeyboardButton(list[i]), new KeyboardButton(list[i + 1]) });
            else
                rows.Add(new[] { new KeyboardButton(list[i]) });
        }

        rows.Add(new[] { new KeyboardButton(BotTexts.Text(language, BotTextKeys.Cancel)) });

        return new ReplyKeyboardMarkup(rows)
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = true
        };
    }
}
