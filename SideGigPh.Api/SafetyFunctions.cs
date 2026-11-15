using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using SideGigPh.AzureSQL;
using SideGigPh.Contracts;

namespace SideGigPh.Api;

/// <summary>Reports and blocks (App Store 1.2, Google Play user-generated content): the caller's own, never on someone's behalf.</summary>
public sealed class SafetyFunctions(FirebaseTokenValidator auth, SafetyStore safety, CallerLimits limits)
{
    /// <summary><c>POST reports</c>: the caller reports a listing, review, chat or person (and blocks them when asked).</summary>
    [Function("SendReport")]
    public async Task<IActionResult> Report(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "reports")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        if (await auth.GetUidAsync(request, cancellationToken) is not { } uid)
        {
            return new UnauthorizedResult();
        }

        if (!limits.TryTake(Limited.Report, uid))
        {
            return ApiResults.TooMany();
        }

        if (await ApiResults.ReadAsync<ReportRequest>(request, cancellationToken) is not { } report)
        {
            return ApiResults.BadRequest();
        }

        return await safety.ReportAsync(uid, report, cancellationToken) == ReportOutcome.Saved ? new NoContentResult() : ApiResults.BadRequest();
    }

    /// <summary><c>PUT blocks/{uid}</c>: the caller blocks that person. Either direction hides the two from each other.</summary>
    [Function("BlockUser")]
    public async Task<IActionResult> Block(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "blocks/{blockedUid}")] HttpRequest request,
        string blockedUid,
        CancellationToken cancellationToken)
    {
        if (await auth.GetUidAsync(request, cancellationToken) is not { } uid)
        {
            return new UnauthorizedResult();
        }

        return await safety.BlockAsync(uid, blockedUid, cancellationToken) ? new NoContentResult() : ApiResults.BadRequest();
    }
}
