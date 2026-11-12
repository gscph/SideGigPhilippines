using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using SideGigPh.Contracts;

namespace SideGigPh.Maui.Services.Api;

/// <summary>
/// <see cref="IPhotoStore"/> on SideGigPh.Api: <c>POST photos</c> uploads the bytes, <c>DELETE photos/{id}</c>
/// removes a photo. The server decides the photo's id (the SHA-256 of the JPEG it stores) and the
/// phone shows the photo straight from its public blob URL, so the WebView caches it like any image.
/// </summary>
public sealed class ApiPhotoStore(ApiClient api, ApiOptions options, ILogger<ApiPhotoStore> logger) : IPhotoStore
{
    // Ids saved in this session, with the URL the server gave back.
    private readonly ConcurrentDictionary<string, string> _urls = new();

    public async Task<string> SaveAsync(byte[] bytes)
    {
        try
        {
            var content = new ByteArrayContent(bytes);
            content.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
            using var response = await api.SendAsync(HttpMethod.Post, "photos", content);
            if (response.StatusCode == HttpStatusCode.Conflict)
            {
                throw new PhotoStoreException("This photo already belongs to another account.", taken: true);
            }

            await api.EnsureWrittenAsync(response, "upload a photo");
            var saved = await response.Content.ReadFromJsonAsync(ApiJson.Default.PhotoDto)
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
            logger.LogError(ex, "Photo upload failed.");
            throw new PhotoStoreException("The photo could not be uploaded.", ex);
        }
    }

    public Task<string?> GetUrlAsync(string photoId) =>
        Task.FromResult<string?>(_urls.TryGetValue(photoId, out var url) ? url : $"{options.PhotoBaseUrl}{photoId}.jpg");

    public async Task DeleteAsync(string photoId)
    {
        _urls.TryRemove(photoId, out _);
        try
        {
            using var response = await api.SendAsync(HttpMethod.Delete, $"photos/{ApiClient.Escape(photoId)}");
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Photo delete returned {Status} for {PhotoId}.", (int)response.StatusCode, photoId);
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            // The photo is already out of the listing; a failed delete only leaves an unlisted file
            // behind, and must not stop the user's change (or Start over) half way.
            logger.LogWarning(ex, "Photo delete failed for {PhotoId}.", photoId);
        }
    }
}
