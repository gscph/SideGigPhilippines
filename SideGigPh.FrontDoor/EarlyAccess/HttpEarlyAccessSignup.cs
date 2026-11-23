using System.Net;
using System.Net.Http.Json;
using SideGigPh.Contracts;

namespace SideGigPh.FrontDoor.EarlyAccess;

/// <summary>Sends the sign-up once to the API's <c>POST early-access</c> (no automatic retry: each one is an email).</summary>
public sealed class HttpEarlyAccessSignup(HttpClient http) : IEarlyAccessSignup
{
    /// <summary>A cold API (Flex Consumption) can take a while to wake; after this the form says "didn't send".</summary>
    public static readonly TimeSpan SendLimit = TimeSpan.FromSeconds(30);

    public async Task<SignupResult> JoinAsync(EarlyAccessRequest request, CancellationToken ct = default)
    {
        using var limit = CancellationTokenSource.CreateLinkedTokenSource(ct);
        limit.CancelAfter(SendLimit);
        try
        {
            using var response = await http.PostAsJsonAsync("early-access", request, ApiJson.Default.EarlyAccessRequest, limit.Token);
            return response.StatusCode switch
            {
                _ when response.IsSuccessStatusCode => SignupResult.Sent,
                HttpStatusCode.BadRequest => await IsBadEmailAsync(response, limit.Token) ? SignupResult.InvalidEmail : SignupResult.Failed,
                HttpStatusCode.TooManyRequests => SignupResult.TooManyTries,
                _ => SignupResult.Failed,
            };
        }
        catch (HttpRequestException)
        {
            // No answer the page may read. The form checked the browser was online just before, so this is a dropped
            // connection or the API refusing this site (CORS): "didn't send, try again", never "No internet".
            return SignupResult.Failed;
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return SignupResult.Failed;
        }
    }

    /// <summary>Only <c>invalid-email</c> blames the email; any other 400 (a bad phone, a bad body) is "didn't send".</summary>
    private static async Task<bool> IsBadEmailAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            var error = await response.Content.ReadFromJsonAsync(ApiJson.Default.ApiError, ct);
            return error?.Error == ApiErrors.Invalid("email");
        }
        catch (System.Text.Json.JsonException)
        {
            return false;
        }
    }
}
