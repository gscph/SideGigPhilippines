using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.Logging;

namespace SideGigPh.Maui.Services.Api;

/// <summary>Where SideGigPh.Api and the photo files live. Neither is a secret.</summary>
/// <param name="ApiUrl">The Azure Functions API, e.g. <c>https://sidegigphapi-hrb9bgerbgcgawbm.southeastasia-01.azurewebsites.net/api/</c>.</param>
/// <param name="PhotoBaseUrl">The public blob container, e.g. <c>https://sidegigphbfb9.blob.core.windows.net/photos/</c>.</param>
/// <param name="Handler">Leave null in the app. Tests pass one to talk to an in-process API instead of the network.</param>
/// <param name="ClientVersion">This app build (<c>BuildInfo.ClientHeader</c>), sent on every call as <c>X-App-Version</c>.</param>
public sealed record ApiOptions(string ApiUrl, string PhotoBaseUrl, HttpMessageHandler? Handler = null, string? ClientVersion = null);

/// <summary>
/// The one HTTP connection to SideGigPh.Api. Every request carries the signed-in user's ID token
/// (<see cref="IIdentityProvider.GetIdTokenAsync"/>), which the API checks; JSON uses the shared
/// <c>SideGigPh.Contracts.ApiJson</c> types. Every request also names the app build (<c>X-App-Version</c>) and a
/// failed one is logged with the API build that answered (<c>X-Api-Version</c>), so an error report says which
/// versions met.
/// </summary>
public sealed class ApiClient(ApiOptions options, IIdentityProvider identity, ILogger<ApiClient> logger)
{
    private readonly HttpClient _http = CreateHttp(options);

    /// <summary>Sends the request with the sign-in token. The caller disposes the response.</summary>
    public async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, HttpContent? content = null, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(method, path) { Content = content };
        if (await identity.GetIdTokenAsync() is { } token)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await _http.SendAsync(request, cancellationToken);
    }

    /// <summary>The JSON answer to a GET, or null for 404. Throws for any other failure.</summary>
    public async Task<T?> GetAsync<T>(string path, JsonTypeInfo<T> type, CancellationToken cancellationToken = default) where T : class
    {
        using var response = await SendAsync(HttpMethod.Get, path, cancellationToken: cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await ThrowIfFailedAsync(response, $"read {path}");
        return await response.Content.ReadFromJsonAsync(type, cancellationToken);
    }

    /// <summary>
    /// A GET that downloads only when the server's copy changed: sends <paramref name="knownVersion"/> as
    /// <c>If-None-Match</c>. Answers the new value and its version (the <c>ETag</c>), or a null value with the known
    /// version when the server says 304 Not Modified. Throws for any failure.
    /// </summary>
    public async Task<(T? Value, string? Version)> GetIfChangedAsync<T>(string path, string? knownVersion, JsonTypeInfo<T> type, CancellationToken cancellationToken)
        where T : class
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        if (knownVersion is not null && EntityTagHeaderValue.TryParse(knownVersion, out var tag))
        {
            request.Headers.IfNoneMatch.Add(tag);
        }

        using var response = await _http.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotModified)
        {
            return (null, response.Headers.ETag?.ToString() ?? knownVersion);
        }

        await ThrowIfFailedAsync(response, $"read {path}");
        return (await response.Content.ReadFromJsonAsync(type, cancellationToken), response.Headers.ETag?.ToString());
    }

    /// <summary>
    /// Sends <paramref name="body"/> as JSON with a Content-Length (not streamed/chunked, which the Functions host
    /// may hand over as an empty body). The caller checks the status and disposes the response.
    /// </summary>
    public Task<HttpResponseMessage> SendJsonAsync<T>(HttpMethod method, string path, T body, JsonTypeInfo<T> type)
    {
        var content = new ByteArrayContent(JsonSerializer.SerializeToUtf8Bytes(body, type));
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json") { CharSet = "utf-8" };
        return SendAsync(method, path, content);
    }

    /// <summary>
    /// A read that can't reach the API (offline, server error) shows as "nothing there", logged: the screens
    /// already have empty states. Writes don't use this: they throw, and the pages say "didn't send / didn't publish".
    /// </summary>
    public async Task<T> ReadAsync<T>(Func<Task<T>> read, T fallback, string what)
    {
        try
        {
            return await read();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            logger.LogError(ex, "API: couldn't read {What}.", what);
            return fallback;
        }
    }

    /// <summary>
    /// Throws (logged) unless the write succeeded. The log and the exception carry the server's whole error answer
    /// (its code and, while the API sends them, the exception and stack trace).
    /// </summary>
    public Task EnsureWrittenAsync(HttpResponseMessage response, string what) => ThrowIfFailedAsync(response, what);

    private async Task ThrowIfFailedAsync(HttpResponseMessage response, string what)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync();
        var apiVersion = ApiVersionOf(response);
        logger.LogError("API {ApiVersion}: couldn't {What}: {Status} {Reason}\n{Body}", apiVersion, what, (int)response.StatusCode, response.ReasonPhrase, body);
        throw new HttpRequestException(
            $"API {apiVersion}: couldn't {what}: {(int)response.StatusCode} {response.ReasonPhrase}. {body}", null, response.StatusCode);
    }

    /// <summary>The API build that answered (<c>X-Api-Version</c>), or "unknown" (an older API, or an answer from Azure's front door).</summary>
    public static string ApiVersionOf(HttpResponseMessage response) =>
        response.Headers.TryGetValues(SideGigPh.Contracts.ApiHeaders.ApiVersion, out var values) && values.FirstOrDefault() is { Length: > 0 } version
            ? version
            : Diagnostics.BuildInfo.Unknown;

    public static string Escape(string value) => Uri.EscapeDataString(value);

    // The platform's default handler in the app, asking for gzip answers (the API compresses JSON); a test handler only
    // when one is given.
    private static HttpClient CreateHttp(ApiOptions options)
    {
        var http = options.Handler is null ? new HttpClient(GzipHandler()) : new HttpClient(options.Handler, disposeHandler: false);
        http.BaseAddress = new Uri(options.ApiUrl);
        http.Timeout = TimeSpan.FromSeconds(60);
        if (!string.IsNullOrWhiteSpace(options.ClientVersion))
        {
            http.DefaultRequestHeaders.TryAddWithoutValidation(SideGigPh.Contracts.ApiHeaders.AppVersion, options.ClientVersion);
        }

        return http;
    }

    /// <summary>
    /// The platform's handler (Android's and iOS's native HTTP stacks behind <see cref="HttpClientHandler"/>) sending
    /// <c>Accept-Encoding: gzip</c> and unzipping answers. A platform that can't is left as it is: the API then
    /// answers uncompressed.
    /// </summary>
    internal static HttpClientHandler GzipHandler()
    {
        var handler = new HttpClientHandler();
        try
        {
            handler.AutomaticDecompression = DecompressionMethods.GZip;
        }
        catch (PlatformNotSupportedException)
        {
            // iOS's NSUrlSession unzips by itself.
        }

        return handler;
    }
}
