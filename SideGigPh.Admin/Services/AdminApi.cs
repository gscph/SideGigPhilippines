using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization.Metadata;
using SideGigPh.Contracts;

namespace SideGigPh.Admin.Services;

/// <summary>
/// The site's one way to SideGigPh.Api (base address <c>Api:BaseUrl</c>, else the page's own <c>../</c>): the signed-in
/// account's Firebase ID token as Bearer on every call, JSON through the shared source-generated <see cref="ApiJson"/>.
/// Transport only: each feature's service (<c>Components/&lt;Feature&gt;/&lt;Feature&gt;Service</c>) says what to ask.
/// Failures throw <see cref="AdminApiException"/> with a message the page can show.
/// </summary>
public sealed class AdminApi(HttpClient http, IAdminSignIn signIn)
{
    public async Task<T> GetAsync<T>(string path, JsonTypeInfo<T> type, CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(HttpMethod.Get, path, null, cancellationToken);
        await EnsureOkAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync(type, cancellationToken)
               ?? throw new AdminApiException("The API answered with nothing. Try again.");
    }

    /// <summary>An image the API sends (e.g. a proof photo) as a <c>data:</c> URL, or null when it is gone (404).</summary>
    public async Task<string?> GetImageAsync(string path, CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(HttpMethod.Get, path, null, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureOkAsync(response, cancellationToken);
        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        var type = response.Content.Headers.ContentType?.MediaType ?? "image/jpeg";
        return bytes.Length == 0 ? null : $"data:{type};base64,{Convert.ToBase64String(bytes)}";
    }

    public async Task PostAsync<TBody>(string path, TBody body, JsonTypeInfo<TBody> type, CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(HttpMethod.Post, path, JsonContent.Create(body, type), cancellationToken);
        await EnsureOkAsync(response, cancellationToken);
    }

    /// <summary>A change with a JSON body (<c>POST</c>, <c>PUT</c>) whose answer is JSON, e.g. the catalog after the change.</summary>
    public async Task<TResult> SendAsync<TBody, TResult>(HttpMethod method, string path, TBody body, JsonTypeInfo<TBody> bodyType,
        JsonTypeInfo<TResult> resultType, CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(method, path, JsonContent.Create(body, bodyType), cancellationToken);
        return await ReadAsync(response, resultType, cancellationToken);
    }

    /// <summary>A change with no body (<c>DELETE</c>, a <c>POST</c> like <c>…/restore</c>) whose answer is JSON.</summary>
    public async Task<TResult> SendAsync<TResult>(HttpMethod method, string path, JsonTypeInfo<TResult> resultType, CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(method, path, null, cancellationToken);
        return await ReadAsync(response, resultType, cancellationToken);
    }

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response, JsonTypeInfo<T> type, CancellationToken cancellationToken)
    {
        await EnsureOkAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync(type, cancellationToken)
               ?? throw new AdminApiException("The API answered with nothing. Try again.");
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, HttpContent? content, CancellationToken cancellationToken)
    {
        var token = await signIn.IdTokenAsync() ?? throw new AdminApiException("You're signed out. Sign in again.");
        using var request = new HttpRequestMessage(method, path) { Content = content };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        try
        {
            return await http.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            // In a browser a CORS refusal looks exactly like no connection.
            throw new AdminApiException(
                "Can't reach the API. Check the internet, and that the Function App's CORS allows this site's address.", ex);
        }
    }

    private static async Task EnsureOkAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var error = response.Content.Headers.ContentType?.MediaType == "application/json"
            ? await response.Content.ReadFromJsonAsync(ApiJson.Default.ApiError, cancellationToken)
            : null;
        var code = error?.Error;
        var status = (int)response.StatusCode;
        throw new AdminApiException(response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => "The API didn't accept the sign-in. Sign out and in again.",
            HttpStatusCode.Forbidden when code == ApiErrors.NotAdmin => "This account isn't an operator.",
            HttpStatusCode.Conflict when code == ApiErrors.OthersStays => "A trade's \"Others\" job can be renamed, but never retired.",
            HttpStatusCode.BadRequest when error?.Detail is { Length: > 0 } field => $"The API didn't accept \"{field}\". Check it and try again.",
            HttpStatusCode.NotFound => "Not there any more (already decided or removed?). Reload.",
            HttpStatusCode.TooManyRequests => "Too many tries. Wait a minute.",
            _ when code is null => $"The API answered {status}. Try again.",
            _ => $"The API answered {status} ({code}). Try again.",
        });
    }
}

/// <summary>An API call that didn't work, with a message for the operator.</summary>
public sealed class AdminApiException(string message, Exception? inner = null) : Exception(message, inner);
