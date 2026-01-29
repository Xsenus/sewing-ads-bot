using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SewingAdsBot.Api.Services;

/// <summary>
/// Получение метаданных Telegram-бота через Bot API.
/// </summary>
public sealed class TelegramBotMetadataService
{
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Конструктор.
    /// </summary>
    public TelegramBotMetadataService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Скачать данные бота и основные свойства.
    /// </summary>
    public async Task<TelegramBotMetadata> FetchAsync(string token, CancellationToken ct = default)
    {
        var baseUrl = $"https://api.telegram.org/bot{token}";
        var me = await GetAsync<TelegramUserResponse>($"{baseUrl}/getMe", ct);

        var description = await GetOptionalAsync<TelegramDescriptionResponse>($"{baseUrl}/getMyDescription", ct);
        var shortDescription = await GetOptionalAsync<TelegramShortDescriptionResponse>($"{baseUrl}/getMyShortDescription", ct);
        var commands = await GetOptionalAsync<TelegramCommandsResponse>($"{baseUrl}/getMyCommands", ct);

        var photoFileId = await GetPhotoFileIdAsync(baseUrl, me.Result.Id, ct);
        var photoFilePath = string.IsNullOrWhiteSpace(photoFileId)
            ? null
            : await GetPhotoFilePathAsync(baseUrl, photoFileId, ct);

        var commandsJson = commands?.Result == null
            ? null
            : JsonSerializer.Serialize(commands.Result, JsonOptions);

        return new TelegramBotMetadata(
            me.Result.Id,
            me.Result.FirstName,
            me.Result.Username,
            description?.Result.Description,
            shortDescription?.Result.ShortDescription,
            commandsJson,
            photoFileId,
            photoFilePath);
    }

    /// <summary>
    /// Скачать файл аватарки бота.
    /// </summary>
    public async Task<Stream?> FetchPhotoStreamAsync(string token, string filePath, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return null;

        var fileUrl = $"https://api.telegram.org/file/bot{token}/{filePath}";
        var response = await _httpClient.GetAsync(fileUrl, ct);
        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadAsStreamAsync(ct);
    }

    private async Task<string?> GetPhotoFileIdAsync(string baseUrl, long userId, CancellationToken ct)
    {
        var photos = await GetOptionalAsync<TelegramProfilePhotosResponse>(
            $"{baseUrl}/getUserProfilePhotos?user_id={userId}&limit=1",
            ct);

        return photos?.Result.Photos?.FirstOrDefault()?.FirstOrDefault()?.FileId;
    }

    private async Task<string?> GetPhotoFilePathAsync(string baseUrl, string fileId, CancellationToken ct)
    {
        var file = await GetOptionalAsync<TelegramFileResponse>($"{baseUrl}/getFile?file_id={fileId}", ct);
        return file?.Result.FilePath;
    }

    private async Task<T> GetAsync<T>(string url, CancellationToken ct)
    {
        var response = await _httpClient.GetFromJsonAsync<T>(url, JsonOptions, ct);
        if (response == null)
            throw new InvalidOperationException("Ответ Telegram API пуст.");

        if (response is TelegramBaseResponse baseResponse && !baseResponse.Ok)
            throw new InvalidOperationException(baseResponse.Description ?? "Ошибка Telegram API.");

        return response;
    }

    private async Task<T?> GetOptionalAsync<T>(string url, CancellationToken ct) where T : TelegramBaseResponse
    {
        try
        {
            return await GetAsync<T>(url, ct);
        }
        catch
        {
            return null;
        }
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private abstract record TelegramBaseResponse
    {
        public bool Ok { get; init; }
        public string? Description { get; init; }
    }

    private sealed record TelegramUserResponse : TelegramBaseResponse
    {
        public TelegramUser Result { get; init; } = new();
    }

    private sealed record TelegramDescriptionResponse : TelegramBaseResponse
    {
        public TelegramDescription Result { get; init; } = new();
    }

    private sealed record TelegramShortDescriptionResponse : TelegramBaseResponse
    {
        public TelegramShortDescription Result { get; init; } = new();
    }

    private sealed record TelegramCommandsResponse : TelegramBaseResponse
    {
        public List<TelegramCommand> Result { get; init; } = new();
    }

    private sealed record TelegramProfilePhotosResponse : TelegramBaseResponse
    {
        public TelegramProfilePhotos Result { get; init; } = new();
    }

    private sealed record TelegramFileResponse : TelegramBaseResponse
    {
        public TelegramFile Result { get; init; } = new();
    }

    private sealed record TelegramUser
    {
        public long Id { get; init; }

        [JsonPropertyName("first_name")]
        public string? FirstName { get; init; }

        public string? Username { get; init; }
    }

    private sealed record TelegramDescription
    {
        public string? Description { get; init; }
    }

    private sealed record TelegramShortDescription
    {
        [JsonPropertyName("short_description")]
        public string? ShortDescription { get; init; }
    }

    private sealed record TelegramCommand
    {
        public string? Command { get; init; }
        public string? Description { get; init; }
    }

    private sealed record TelegramProfilePhotos
    {
        public List<List<TelegramPhotoSize>>? Photos { get; init; }
    }

    private sealed record TelegramPhotoSize
    {
        [JsonPropertyName("file_id")]
        public string? FileId { get; init; }
    }

    private sealed record TelegramFile
    {
        [JsonPropertyName("file_path")]
        public string? FilePath { get; init; }
    }
}

/// <summary>
/// DTO метаданных бота.
/// </summary>
public sealed record TelegramBotMetadata(
    long TelegramId,
    string? Name,
    string? Username,
    string? Description,
    string? ShortDescription,
    string? CommandsJson,
    string? PhotoFileId,
    string? PhotoFilePath);
