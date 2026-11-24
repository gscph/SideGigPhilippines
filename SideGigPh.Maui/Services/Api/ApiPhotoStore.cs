using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using SideGigPh.Contracts;
using SideGigPh.Contracts.Photos;

namespace SideGigPh.Maui.Services.Api;

/// <summary>
/// <see cref="IPhotoStore"/> on SideGigPh.Api: <c>POST photos</c> uploads the bytes, <c>DELETE photos/{id}</c>
/// removes a photo; a worker's selfie goes to <c>POST photos/selfie</c>, which checks it instead of re-encoding it
/// (the phone already made it small). The server decides the photo's id (a new random one per upload) and the
/// phone shows the photo straight from its public blob URL, so the WebView caches it like any image.
/// </summary>
public sealed class ApiPhotoStore(ApiClient api, ApiOptions options, ILogger<ApiPhotoStore> logger) : IPhotoStore
{
    // Ids saved in this session, with the URL the server gave back.
    private readonly ConcurrentDictionary<string, string> _urls = new();

    public Task<string> SaveAsync(byte[] bytes) => UploadAsync("photos", bytes);

    /// <summary>
    /// Checks the selfie with the server's own rules first (<see cref="SelfieJpegRules"/>): one the server would refuse
    /// is never sent, and the person is asked to take it again (<see cref="PhotoStoreFailure.Rejected"/>).
    /// </summary>
    public Task<string> SaveSelfieAsync(byte[] bytes)
    {
        var verdict = SelfieJpegRules.Check(bytes).Verdict;
        if (verdict != SelfieVerdict.Ok)
        {
            logger.LogError("Selfie not sent: {Verdict} ({Bytes} bytes).", verdict, bytes.Length);
            throw new PhotoStoreException($"The selfie doesn't meet the upload rules ({verdict}).", PhotoStoreFailure.Rejected);
        }

        return UploadAsync("photos/selfie", bytes);
    }

    private async Task<string> UploadAsync(string path, byte[] bytes)
    {
        try
        {
            // A resend after a dropped connection at worst leaves one unused file.
            using var response = await api.SendAsync(HttpMethod.Post, path, bytes, "image/jpeg");
            await api.EnsureWrittenAsync(response, "upload a photo");
            var saved = await response.Content.ReadFromJsonAsync(ApiJson.Default.PhotoDto)
                ?? throw new PhotoStoreException("The photo server sent an empty answer.", PhotoStoreFailure.ServerError);

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
            throw new PhotoStoreException("The photo could not be uploaded.", FailureOf(ex), ex);
        }
    }

    /// <summary>
    /// Why an upload failed, for an honest message: no answer at all is no internet; the server refusing the photo
    /// itself (400 broken or unreadable picture, 413 too big, 415) needs another photo; anything else it answered
    /// (401, 429, 5xx, a broken answer) is worth sending the same photo again later.
    /// </summary>
    public static PhotoStoreFailure FailureOf(Exception ex) =>
        NetworkErrors.IsNetwork(ex) ? PhotoStoreFailure.NoInternet
        : ex is HttpRequestException { StatusCode: HttpStatusCode.BadRequest or HttpStatusCode.RequestEntityTooLarge or HttpStatusCode.UnsupportedMediaType }
            ? PhotoStoreFailure.Rejected
        : PhotoStoreFailure.ServerError;

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
