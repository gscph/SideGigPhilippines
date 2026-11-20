using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using SideGigPh.Contracts;

namespace SideGigPh.Admin.Services;

/// <summary>
/// The operator's calls to SideGigPh.Api (base address <c>Api:BaseUrl</c> in <c>wwwroot/appsettings.json</c>): the
/// signed-in account's Firebase ID token as Bearer on every call, JSON through the shared source-generated
/// <see cref="ApiJson"/>. Failures throw <see cref="AdminApiException"/> with a message the page can show.
/// </summary>
public sealed class AdminApi(HttpClient http, IAdminSignIn signIn)
{
    /// <summary><c>GET admin/me</c>: whether this account is in the API's <c>AdminUids</c> setting.</summary>
    public async Task<bool> IsOperatorAsync()
    {
        using var response = await SendAsync(HttpMethod.Get, "admin/me");
        await EnsureOkAsync(response);
        return (await response.Content.ReadFromJsonAsync(ApiJson.Default.AdminStatusDto))?.IsOperator == true;
    }

    /// <summary><c>GET admin/verifications</c>: the badge requests waiting, oldest first.</summary>
    public async Task<IReadOnlyList<AdminVerificationDto>> PendingAsync(int take)
    {
        using var response = await SendAsync(HttpMethod.Get, $"admin/verifications?take={take}");
        await EnsureOkAsync(response);
        return await response.Content.ReadFromJsonAsync(ApiJson.Default.ListAdminVerificationDto) ?? [];
    }

    /// <summary>
    /// <c>GET admin/verifications/{id}/document</c>: the proof photo as an image source (the API logs every look),
    /// or null when it is gone.
    /// </summary>
    public async Task<string?> DocumentAsync(long id)
    {
        using var response = await SendAsync(HttpMethod.Get, $"admin/verifications/{id}/document");
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureOkAsync(response);
        var bytes = await response.Content.ReadAsByteArrayAsync();
        var type = response.Content.Headers.ContentType?.MediaType ?? "image/jpeg";
        return bytes.Length == 0 ? null : $"data:{type};base64,{Convert.ToBase64String(bytes)}";
    }

    /// <summary><c>POST admin/verifications/{id}/decision</c>. The API deletes the photo either way.</summary>
    public async Task DecideAsync(long id, bool approve, string? reason)
    {
        using var response = await SendAsync(HttpMethod.Post, $"admin/verifications/{id}/decision",
            JsonContent.Create(new VerificationDecisionRequest(approve, reason), ApiJson.Default.VerificationDecisionRequest));
        await EnsureOkAsync(response);
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, HttpContent? content = null)
    {
        var token = await signIn.IdTokenAsync() ?? throw new AdminApiException("You're signed out. Sign in again.");
        using var request = new HttpRequestMessage(method, path) { Content = content };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        try
        {
            return await http.SendAsync(request);
        }
        catch (HttpRequestException ex)
        {
            // In a browser a CORS refusal looks exactly like no connection.
            throw new AdminApiException(
                "Can't reach the API. Check the internet, and that the Function App's CORS allows this site's address.", ex);
        }
    }

    private static async Task EnsureOkAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var code = response.Content.Headers.ContentType?.MediaType == "application/json"
            ? (await response.Content.ReadFromJsonAsync(ApiJson.Default.ApiError))?.Error
            : null;
        var status = (int)response.StatusCode;
        throw new AdminApiException(response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => "The API didn't accept the sign-in. Sign out and in again.",
            HttpStatusCode.Forbidden when code == ApiErrors.NotAdmin => "This account isn't an operator.",
            HttpStatusCode.NotFound => "That request is gone (already decided?). Reload the list.",
            HttpStatusCode.TooManyRequests => "Too many tries. Wait a minute.",
            _ when code is null => $"The API answered {status}. Try again.",
            _ => $"The API answered {status} ({code}). Try again.",
        });
    }
}

/// <summary>An API call that didn't work, with a message for the operator.</summary>
public sealed class AdminApiException(string message, Exception? inner = null) : Exception(message, inner);
