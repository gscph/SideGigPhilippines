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
                HttpStatusCode.BadRequest => SignupResult.InvalidEmail,
                HttpStatusCode.TooManyRequests => SignupResult.TooManyTries,
                _ => SignupResult.Failed,
            };
        }
        catch (HttpRequestException)
        {
            // No answer at all: the browser couldn't reach the API (offline, or the connection dropped).
            return SignupResult.Offline;
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return SignupResult.Failed;
        }
    }
}
