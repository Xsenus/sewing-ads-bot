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
    public static ReplyKeyboardMarkup MainMenu()
        => new(new[]
        {
            new KeyboardButton[] { "Создать объявление", "Найти объявление" },
            new KeyboardButton[] { "Мой профиль", "Помощь" },
            new KeyboardButton[] { "Платное объявление" }
        })
        {
            ResizeKeyboard = true
        };

    /// <summary>
    /// Меню профиля (ReplyKeyboard).
    /// </summary>
    public static ReplyKeyboardMarkup ProfileMenu()
        => new(new[]
        {
            new KeyboardButton[] { "Место", "Мои объявления" },
            new KeyboardButton[] { "Реферальная ссылка", "Назад" }
        })
        {
            ResizeKeyboard = true
        };

    /// <summary>
    /// Клавиатура предпросмотра бесплатного объявления.
/// </summary>
    public static InlineKeyboardMarkup PreviewFree(Guid adId)
        => new(new[]
        {
            new []
            {
                InlineKeyboardButton.WithCallbackData("✅ Опубликовать", $"create:publish:{adId}"),
            },
            new []
            {
                InlineKeyboardButton.WithCallbackData("✏️ Заголовок", $"create:edit:title:{adId}"),
                InlineKeyboardButton.WithCallbackData("✏️ Текст", $"create:edit:text:{adId}")
            },
            new []
            {
                InlineKeyboardButton.WithCallbackData("✏️ Контакты", $"create:edit:contacts:{adId}"),
                InlineKeyboardButton.WithCallbackData("❌ Отменить", "create:cancel")
            }
        });

    /// <summary>
    /// Клавиатура предпросмотра платного объявления.
/// </summary>
    public static InlineKeyboardMarkup PreviewPaid(Guid adId)
        => new(new[]
        {
            new []
            {
                InlineKeyboardButton.WithCallbackData("💳 Оплатить и опубликовать", $"create:pay:{adId}"),
            },
            new []
            {
                InlineKeyboardButton.WithCallbackData("✏️ Заголовок", $"create:edit:title:{adId}"),
                InlineKeyboardButton.WithCallbackData("✏️ Текст", $"create:edit:text:{adId}")
            },
            new []
            {
                InlineKeyboardButton.WithCallbackData("✏️ Контакты", $"create:edit:contacts:{adId}"),
                InlineKeyboardButton.WithCallbackData("🖼 Медиа", $"create:edit:media:{adId}")
            },
            new []
            {
                InlineKeyboardButton.WithCallbackData("❌ Отменить", "create:cancel")
            }
        });

    /// <summary>
    /// Клавиатура выбора типа объявления.
/// </summary>
    public static InlineKeyboardMarkup AdType()
        => new(new[]
        {
            new []
            {
                InlineKeyboardButton.WithCallbackData("Бесплатное", "type:free"),
                InlineKeyboardButton.WithCallbackData("Платное", "type:paid")
            }
        });

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
    public static ReplyKeyboardMarkup SkipMedia()
        => new(new[]
        {
            new KeyboardButton[] { "Пропустить" },
            new KeyboardButton[] { "Отмена" }
        })
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = true
        };
}
