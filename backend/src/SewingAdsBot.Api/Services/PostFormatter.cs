using SewingAdsBot.Api.Domain.Entities;
using SewingAdsBot.Api.Utilities;

namespace SewingAdsBot.Api.Services;

/// <summary>
/// Формирование текста поста по шаблону.
/// Вынесено в отдельный сервис, чтобы использовать и в публикации, и в модерации.
/// </summary>
public sealed class PostFormatter
{
    /// <summary>
    /// Сформировать текст поста (ParseMode=HTML).
    /// </summary>
    public string BuildPostText(Ad ad, Category category, Channel channel)
    {
        var countryTag = TextSlug.ToHashtag(ad.Country);
        var cityTag = TextSlug.ToHashtag(ad.City);
        var catTag = TextSlug.ToHashtag(category.Slug);

        var title = HtmlUtil.Escape(ad.Title);
        var body = HtmlUtil.Escape(ad.Text);
        var contacts = HtmlUtil.Escape(ad.Contacts);

        var footerText = HtmlUtil.Escape(channel.FooterLinkText);
        var footerUrl = HtmlUtil.Escape(channel.FooterLinkUrl);

        return $"{countryTag} {cityTag}\n\n" +
               $"<b>{title}</b>\n\n" +
               $"{body}\n\n" +
               $"<b>Контакты:</b> {contacts}\n\n" +
               $"{catTag}\n" +
               $"{footerText} ({footerUrl})";
    }

    /// <summary>
    /// Сформировать текст предпросмотра для модератора (в личку).
    /// </summary>
    public string BuildModerationPreview(Ad ad, Category category, Channel channel)
    {
        var header = $"<b>Модерация</b>\nКанал: <b>{HtmlUtil.Escape(channel.Title)}</b>\n\n";
        return header + BuildPostText(ad, category, channel);
    }
}
