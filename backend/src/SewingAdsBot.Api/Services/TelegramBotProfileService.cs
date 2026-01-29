using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;

namespace SewingAdsBot.Api.Services;

/// <summary>
/// Обновление профиля Telegram-бота через Bot API.
/// </summary>
public sealed class TelegramBotProfileService
{
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Конструктор.
    /// </summary>
    public TelegramBotProfileService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Обновить данные профиля: имя, описания, команды.
    /// </summary>
    public async Task ApplyProfileAsync(string token, TelegramBotProfileUpdate update, CancellationToken ct = default)
    {
        var baseUrl = $"https://api.telegram.org/bot{token}";

        if (!string.IsNullOrWhiteSpace(update.Name))
            await PostAsync(baseUrl, "setMyName", new { name = update.Name }, ct);

        if (update.Description != null)
            await PostAsync(baseUrl, "setMyDescription", new { description = update.Description }, ct);

        if (update.ShortDescription != null)
            await PostAsync(baseUrl, "setMyShortDescription", new { short_description = update.ShortDescription }, ct);

        if (update.Commands != null)
            await PostAsync(baseUrl, "setMyCommands", new { commands = update.Commands }, ct);
    }

    /// <summary>
    /// Установить фотографию профиля.
    /// </summary>
    public async Task SetProfilePhotoAsync(string token, IFormFile file, CancellationToken ct = default)
    {
        var baseUrl = $"https://api.telegram.org/bot{token}";
        await using var stream = file.OpenReadStream();
        var content = new MultipartFormDataContent
        {
            { new StreamContent(stream), "photo", file.FileName }
        };

        var response = await _httpClient.PostAsync($"{baseUrl}/setMyProfilePhoto", content, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<TelegramBaseResponse>(JsonOptions, ct);
        if (result is { Ok: false })
            throw new InvalidOperationException(result.Description ?? "Не удалось обновить фото.");
    }

    private async Task PostAsync(string baseUrl, string method, object payload, CancellationToken ct)
    {
        var response = await _httpClient.PostAsJsonAsync($"{baseUrl}/{method}", payload, JsonOptions, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<TelegramBaseResponse>(JsonOptions, ct);
        if (result is { Ok: false })
            throw new InvalidOperationException(result.Description ?? "Ошибка Telegram API.");
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private sealed record TelegramBaseResponse
    {
        public bool Ok { get; init; }
        public string? Description { get; init; }
    }
}

/// <summary>
/// DTO для обновления профиля бота.
/// </summary>
public sealed record TelegramBotProfileUpdate(
    string? Name,
    string? Description,
    string? ShortDescription,
    List<TelegramCommandPayload>? Commands);

/// <summary>
/// Команда бота для Bot API.
/// </summary>
public sealed record TelegramCommandPayload(string Command, string Description);
