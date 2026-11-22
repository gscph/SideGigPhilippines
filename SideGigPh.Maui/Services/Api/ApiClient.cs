using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.Logging;

namespace SideGigPh.Maui.Services.Api;

/// <summary>Where SideGigPh.Api and the photo files live. Neither is a secret.</summary>
/// <param name="ApiUrl">The Azure Functions API, e.g. <c>https://api.tarbaho.com/api/</c>.</param>
/// <param name="PhotoBaseUrl">The public blob container, e.g. <c>https://sidegigphbfb9.blob.core.windows.net/photos/</c>.</param>
/// <param name="Handler">Leave null in the app. Tests pass one to talk to an in-process API instead of the network.</param>
/// <param name="ClientVersion">This app build (<c>BuildInfo.ClientHeader</c>), sent on every call as <c>X-App-Version</c>.</param>
public sealed record ApiOptions(string ApiUrl, string PhotoBaseUrl, HttpMessageHandler? Handler = null, string? ClientVersion = null);

/// <summary>
/// The one HTTP connection to SideGigPh.Api. Every request carries the signed-in user's ID token
/// (<see cref="IIdentityProvider.GetIdTokenAsync"/>), which the API checks; JSON uses the shared
/// <c>SideGigPh.Contracts.ApiJson</c> types. Every request also names the app build (<c>X-App-Version</c>) and a
/// failed one is logged with the API build that answered (<c>X-Api-Version</c>), so an error report says which
/// versions met. Each request also tells <see cref="INetworkStatus"/> whether it got an answer, so a phone whose network
/// is "connected" but reaches nothing (broken DNS, a captive Wi-Fi) still shows as offline.
/// </summary>
public sealed class ApiClient(ApiOptions options, IIdentityProvider identity, ILogger<ApiClient> logger, INetworkStatus? network = null)
{
    private readonly HttpClient _http = CreateHttp(options);

    /// <summary>
    /// How long one try of a read may take. On mobile data a reused connection can die without a word (the carrier drops
    /// it) and the request then hangs until the timeout: a read gets this long, then goes once more on a fresh connection.
    /// </summary>
    public static readonly TimeSpan ReadTryLimit = TimeSpan.FromSeconds(30);

    /// <summary>A read (GET) is safe to repeat, so one that got no answer goes once more; writes are sent once.</summary>
    public const int ReadTries = 2;

    /// <summary>
    /// Sends the request with the sign-in token. The caller disposes the response. A request that gets no answer is
    /// logged here (so it becomes an error report), unless <paramref name="logFailure"/> is false: only the error-report
    /// send itself passes that, or a failed send would record a report about sending reports.
    /// </summary>
    public async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, HttpContent? content = null,
        CancellationToken cancellationToken = default, bool logFailure = true)
    {
        var token = await identity.GetIdTokenAsync();
        return await SendWithRetryAsync(method, path, logFailure, () =>
        {
            var request = new HttpRequestMessage(method, path) { Content = content };
            if (token is not null)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            return request;
        }, cancellationToken);
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
        using var response = await SendWithRetryAsync(HttpMethod.Get, path, logFailure: true, () =>
        {
            var request = new HttpRequestMessage(HttpMethod.Get, path);
            if (knownVersion is not null && EntityTagHeaderValue.TryParse(knownVersion, out var tag))
            {
                request.Headers.IfNoneMatch.Add(tag);
            }

            return request;
        }, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotModified)
        {
            return (null, response.Headers.ETag?.ToString() ?? knownVersion);
        }

        await ThrowIfFailedAsync(response, $"read {path}");
        return (await response.Content.ReadFromJsonAsync(type, cancellationToken), response.Headers.ETag?.ToString());
    }

    /// <summary>
    /// Sends the request. A connection that drops or times out is an <see cref="HttpRequestException"/> with no status
    /// code on every platform: Android's handler throws <see cref="WebException"/> / <see cref="IOException"/>
    /// ("Socket closed", "Canceled") instead, which reads didn't catch, so a flaky 4G signal broke whole screens.
    /// <para>
    /// This is the one place a request that got no answer is logged (an answer with an error status is logged by
    /// <see cref="ThrowIfFailedAsync"/>), so every failed call reaches the error reports, whatever the caller does with
    /// the exception: pages turn it into "didn't send" and workspaces into a result. The exception is marked reported
    /// (<see cref="Diagnostics.ErrorLog.MarkReported"/>), so a caller that logs it again adds a trail line, not a second
    /// report. Only a cancel the caller asked for is not a failure.
    /// </para>
    /// </summary>
    private async Task<HttpResponseMessage> SendWithRetryAsync(HttpMethod method, string path, bool logFailure,
        Func<HttpRequestMessage> makeRequest, CancellationToken cancellationToken)
    {
        try
        {
            var response = await SendTriesAsync(makeRequest, cancellationToken);
            network?.Answered();
            return response;
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            // A request that ran out of time says nothing about the connection (a cold server, a paused database):
            // only one that couldn't reach the server at all counts the phone as offline.
            if (NetworkErrors.IsNetwork(ex) && !TimedOut(ex))
            {
                network?.NoAnswer();
            }

            if (logFailure)
            {
                logger.LogError(ex, "API: no answer to {Method} {Path}: {Reason}", method, path.Split('?')[0], ex.GetBaseException().Message);
                Diagnostics.ErrorLog.MarkReported(ex); // a caller that logs it again adds a trail line, not a second report
            }

            throw;
        }
    }

    private static bool TimedOut(Exception exception)
    {
        for (var e = exception; e is not null; e = e.InnerException)
        {
            if (e is OperationCanceledException or TimeoutException)
            {
                return true;
            }
        }

        return false;
    }

    private async Task<HttpResponseMessage> SendTriesAsync(Func<HttpRequestMessage> makeRequest, CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            var request = makeRequest();
            if (request.Method != HttpMethod.Get)
            {
                using (request)
                {
                    return await SendOrThrowAsync(request, cancellationToken, cancellationToken);
                }
            }

            using var tryLimit = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            tryLimit.CancelAfter(ReadTryLimit);
            try
            {
                using (request)
                {
                    return await SendOrThrowAsync(request, tryLimit.Token, cancellationToken);
                }
            }
            catch (Exception ex) when (attempt < ReadTries && !cancellationToken.IsCancellationRequested
                && ex is HttpRequestException { StatusCode: null } or OperationCanceledException)
            {
                logger.LogWarning("API: no answer to GET {Path} ({Reason}); trying once more.", request.RequestUri?.OriginalString.Split('?')[0], ex.Message);
            }
        }
    }

    private async Task<HttpResponseMessage> SendOrThrowAsync(HttpRequestMessage request, CancellationToken sendToken, CancellationToken callerToken)
    {
        try
        {
            return await _http.SendAsync(request, sendToken);
        }
        catch (Exception ex) when (ex is WebException or IOException or OperationCanceledException && !callerToken.IsCancellationRequested)
        {
            throw new HttpRequestException($"API: couldn't reach {request.Method} {request.RequestUri?.OriginalString.Split('?')[0]}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Sends <paramref name="body"/> as JSON with a Content-Length (not streamed/chunked, which the Functions host
    /// may hand over as an empty body). The caller checks the status and disposes the response.
    /// </summary>
    public Task<HttpResponseMessage> SendJsonAsync<T>(HttpMethod method, string path, T body, JsonTypeInfo<T> type, bool logFailure = true)
    {
        var content = new ByteArrayContent(JsonSerializer.SerializeToUtf8Bytes(body, type));
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json") { CharSet = "utf-8" };
        return SendAsync(method, path, content, logFailure: logFailure);
    }

    /// <summary>
    /// A read that can't reach the API (offline, server error) shows as "nothing there": the screens already have
    /// empty states. The failed request was already logged by the send (no answer) or <see cref="ThrowIfFailedAsync"/>
    /// (an error status), so only an answer that can't be read is logged here. Writes don't use this: they throw, and
    /// the pages say "didn't send / didn't publish".
    /// </summary>
    public async Task<T> ReadAsync<T>(Func<Task<T>> read, T fallback, string what)
    {
        try
        {
            return await read();
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "API: couldn't read {What}.", what);
            return fallback;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
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
        var failure = new HttpRequestException(
            $"API {apiVersion}: couldn't {what}: {(int)response.StatusCode} {response.ReasonPhrase}. {body}", null, response.StatusCode);
        Diagnostics.ErrorLog.MarkReported(failure);
        throw failure;
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
