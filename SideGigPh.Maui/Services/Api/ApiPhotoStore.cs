using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace SideGigPh.Maui.Services.Api;

/// <summary>Where the photo API and the photo files live. Neither is a secret.</summary>
/// <param name="ApiUrl">The Azure Functions API, e.g. <c>https://sidegigphapi-hrb9bgerbgcgawbm.southeastasia-01.azurewebsites.net/api/</c>.</param>
/// <param name="PhotoBaseUrl">The public blob container, e.g. <c>https://sidegigphbfb9.blob.core.windows.net/photos/</c>.</param>
public sealed record PhotoApiOptions(string ApiUrl, string PhotoBaseUrl);

/// <summary>
/// <see cref="IPhotoStore"/> on <c>SideGigPh.Api</c>: <c>POST photos</c> uploads the bytes, <c>DELETE photos/{id}</c>
/// removes a photo. The server decides the photo's id (the SHA-256 of the JPEG it stores) and the
/// phone shows the photo straight from its public blob URL, so the WebView caches it like any image.
/// Each call carries the signed-in user's ID token (<see cref="IIdentityProvider.GetIdTokenAsync"/>).
/// </summary>
public sealed class ApiPhotoStore : IPhotoStore
{
    private readonly HttpClient _http;
    private readonly string _photoBaseUrl;
    private readonly IIdentityProvider _identity;
    private readonly ILogger<ApiPhotoStore> _logger;

    // Ids saved in this session, with the URL the server gave back.
    private readonly ConcurrentDictionary<string, string> _urls = new();

    public ApiPhotoStore(PhotoApiOptions options, IIdentityProvider identity, ILogger<ApiPhotoStore> logger)
    {
        _http = new HttpClient { BaseAddress = new Uri(options.ApiUrl), Timeout = TimeSpan.FromSeconds(60) };
        _photoBaseUrl = options.PhotoBaseUrl;
        _identity = identity;
        _logger = logger;
    }

    public async Task<string> SaveAsync(byte[] bytes)
    {
        try
        {
            using var request = await NewRequestAsync(HttpMethod.Post, "photos");
            request.Content = new ByteArrayContent(bytes);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");

            using var response = await _http.SendAsync(request);
            if (response.StatusCode == HttpStatusCode.Conflict)
            {
                throw new PhotoStoreException("This photo already belongs to another account.");
            }

            response.EnsureSuccessStatusCode();
            var saved = await response.Content.ReadFromJsonAsync(PhotoApiJson.Default.PhotoSavedDto)
                ?? throw new PhotoStoreException("The photo server sent an empty answer.");

            _urls[saved.Id] = saved.Url;
            return saved.Id;
        }
        catch (PhotoStoreException)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            _logger.LogError(ex, "Photo upload failed.");
            throw new PhotoStoreException("The photo could not be uploaded.", ex);
        }
    }

    public Task<string?> GetUrlAsync(string photoId) =>
        Task.FromResult<string?>(_urls.TryGetValue(photoId, out var url) ? url : $"{_photoBaseUrl}{photoId}.jpg");

    public async Task DeleteAsync(string photoId)
    {
        _urls.TryRemove(photoId, out _);
        try
        {
            using var request = await NewRequestAsync(HttpMethod.Delete, $"photos/{Uri.EscapeDataString(photoId)}");
            using var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Photo delete returned {Status} for {PhotoId}.", (int)response.StatusCode, photoId);
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            // The photo is already out of the listing; a failed delete only leaves an unlisted file
            // behind, and must not stop the user's change (or Start over) half way.
            _logger.LogWarning(ex, "Photo delete failed for {PhotoId}.", photoId);
        }
    }

    private async Task<HttpRequestMessage> NewRequestAsync(HttpMethod method, string path)
    {
        var request = new HttpRequestMessage(method, path);
        if (await _identity.GetIdTokenAsync() is { } token)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return request;
    }
}

/// <summary>The API's answer to an upload: <c>{ "id": "…", "url": "…" }</c>.</summary>
internal sealed record PhotoSavedDto(string Id, string Url);

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(PhotoSavedDto))]
internal sealed partial class PhotoApiJson : JsonSerializerContext;
