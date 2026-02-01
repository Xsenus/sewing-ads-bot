using Microsoft.Extensions.Options;
using SewingAdsBot.Api.Domain.Entities;
using SewingAdsBot.Api.Options;
using SewingAdsBot.Api.Utilities;

namespace SewingAdsBot.Api.Services;

/// <summary>
/// Формирование текста поста по шаблону.
/// Вынесено в отдельный сервис, чтобы использовать и в публикации, и в модерации.
/// </summary>
public sealed class PostFormatter
{
    private readonly SettingsService _settings;
    private readonly AppOptions _appOptions;

    public PostFormatter(SettingsService settings, IOptions<AppOptions> appOptions)
    {
        _settings = settings;
        _appOptions = appOptions.Value;
    }

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

        var includeLocation = _settings.GetBoolAsync("Post.IncludeLocationTags", true).GetAwaiter().GetResult();
        var includeCategory = _settings.GetBoolAsync("Post.IncludeCategoryTag", true).GetAwaiter().GetResult();
        var includeFooter = _settings.GetBoolAsync("Post.IncludeFooterLink", true).GetAwaiter().GetResult();

        var footerText = HtmlUtil.Escape(channel.FooterLinkText);
        var footerUrl = HtmlUtil.Escape(channel.FooterLinkUrl);

        if (string.IsNullOrWhiteSpace(footerText))
            footerText = HtmlUtil.Escape(_appOptions.DefaultFooterLinkText);
        if (string.IsNullOrWhiteSpace(footerUrl))
            footerUrl = HtmlUtil.Escape(_appOptions.DefaultFooterLinkUrl);

        var lines = new List<string>();

        if (includeLocation)
            lines.Add($"{countryTag} {cityTag}".Trim());

        lines.Add($"<b>{title}</b>");
        lines.Add(body);
        lines.Add($"<b>Контакты:</b> {contacts}");

        if (includeCategory)
            lines.Add(catTag);

        if (includeFooter)
            lines.Add($"{footerText} ({footerUrl})");

        return string.Join("\n\n", lines.Where(x => !string.IsNullOrWhiteSpace(x)));
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
