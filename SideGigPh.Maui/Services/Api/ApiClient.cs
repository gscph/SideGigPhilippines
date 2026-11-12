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
public sealed record ApiOptions(string ApiUrl, string PhotoBaseUrl);

/// <summary>
/// The one HTTP connection to SideGigPh.Api. Every request carries the signed-in user's ID token
/// (<see cref="IIdentityProvider.GetIdTokenAsync"/>), which the API checks; JSON uses the shared
/// <c>SideGigPh.Contracts.ApiJson</c> types.
/// </summary>
public sealed class ApiClient(ApiOptions options, IIdentityProvider identity, ILogger<ApiClient> logger)
{
    private readonly HttpClient _http = new() { BaseAddress = new Uri(options.ApiUrl), Timeout = TimeSpan.FromSeconds(60) };

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

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync(type, cancellationToken);
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

    /// <summary>Throws (logged) unless the write succeeded.</summary>
    public void EnsureWritten(HttpResponseMessage response, string what, string? error = null)
    {
        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("API: couldn't {What}: {Status} {Error}.", what, (int)response.StatusCode, error);
            response.EnsureSuccessStatusCode();
        }
    }

    public static string Escape(string value) => Uri.EscapeDataString(value);
}
