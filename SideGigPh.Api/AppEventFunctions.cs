using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using SideGigPh.AzureSQL;
using SideGigPh.Contracts;

namespace SideGigPh.Api;

/// <summary>The app's queued usage events, uploaded in the background (our own analytics, no personal data).</summary>
public sealed class AppEventFunctions(FirebaseTokenValidator auth, AppEventStore events, CallerLimits limits)
{
    /// <summary>
    /// <c>POST events</c>: a JSON array of at most <see cref="AppEventRules.MaxBatch"/> events the phone queued, only
    /// <see cref="AppEventNames.FromApp"/> names (anything else refuses the whole batch). Who sent them comes from the
    /// token, never the body. An event already kept (the same upload sent again) is skipped. <c>202</c> when kept.
    /// </summary>
    [Function("SendAppEvents")]
    public async Task<IActionResult> Send(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "events")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        if (await auth.GetUidAsync(request, cancellationToken) is not { } uid)
        {
            return new UnauthorizedResult();
        }

        if (!limits.TryTake(Limited.AppEvents, uid))
        {
            return ApiResults.TooMany();
        }

        if (await ApiResults.ReadAsync<List<AppEventDto?>>(request, cancellationToken) is not { } batch)
        {
            return ApiResults.BadRequest();
        }

        return await events.SaveFromAppAsync(uid, batch, cancellationToken)
            ? new AcceptedResult()
            : ApiResults.BadRequest();
    }
}
