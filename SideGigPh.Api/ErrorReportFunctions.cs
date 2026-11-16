using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using SideGigPh.Api.Telemetry;
using SideGigPh.Contracts;

namespace SideGigPh.Api;

/// <summary>The problems the app had on people's phones, sent in the background (diagnostics, no personal data).</summary>
public sealed class ErrorReportFunctions(FirebaseTokenValidator auth, ErrorReportTable reports, CallerLimits limits)
{
    /// <summary>The largest batch read: 20 full reports fit in well under it.</summary>
    public const int MaxBodyBytes = 256 * 1024;

    /// <summary>
    /// <c>POST error-reports</c>: up to <see cref="ErrorReportRules.MaxReports"/> reports from one phone. Signed in only (a
    /// phone keeps what happened before sign-in and sends it after), so nobody can fill the table anonymously; the uid
    /// only counts the limit and is never stored.
    /// </summary>
    [Function("SendErrorReports")]
    public async Task<IActionResult> Send(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "error-reports")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        if (await auth.GetUidAsync(request, cancellationToken) is not { } uid)
        {
            return new UnauthorizedResult();
        }

        if (!limits.TryTake(Limited.ErrorReport, uid))
        {
            return ApiResults.TooMany();
        }

        if (request.ContentLength is null or > MaxBodyBytes
            || await ApiResults.ReadAsync<ErrorReportBatch>(request, cancellationToken) is not { } batch)
        {
            return ApiResults.BadRequest();
        }

        return await reports.SaveAsync(batch, cancellationToken)
            ? new NoContentResult()
            : ApiResults.BadRequest();
    }

    /// <summary>
    /// <c>GET error-reports?take=50&amp;channel=dev</c>: the newest reports, for reading them while building (the
    /// <c>ErrorReports</c> table in Table Storage, without a portal login). <b>Public for now</b>, on the owner's request: the reports are
    /// scrubbed (no tokens, numbers, emails, uids, query strings) and carry no install id, but they do show stack traces.
    /// Put it behind a key or remove it before launch (<c>ApiWiringTests.PublicRoutes</c> lists it).
    /// </summary>
    [Function("ReadErrorReports")]
    public async Task<IActionResult> Read(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "error-reports")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        var take = int.TryParse(request.Query["take"], out var asked) ? asked : 50;
        var channel = request.Query["channel"].ToString() is { Length: > 0 } c ? c : null;
        return new OkObjectResult(await reports.RecentAsync(take, channel, cancellationToken));
    }
}
