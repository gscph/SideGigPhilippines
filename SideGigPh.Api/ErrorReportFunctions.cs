using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using SideGigPh.AzureSQL;
using SideGigPh.Contracts;

namespace SideGigPh.Api;

/// <summary>The problems the app had on people's phones, sent in the background (diagnostics, no personal data).</summary>
public sealed class ErrorReportFunctions(FirebaseTokenValidator auth, ErrorReportStore reports, CallerLimits limits)
{
    /// <summary>
    /// <c>POST error-reports</c>: up to <see cref="ErrorReportRules.MaxReports"/> reports from one phone. Signed in only (a
    /// phone keeps what happened before sign-in and sends it after), so nobody can fill the table anonymously; the uid
    /// only counts the limit and is never stored. A body without a Content-Length (HTTP/2 and HTTP/3 may leave it out:
    /// iPhones through Cloudflare) is read like every other route's, up to <see cref="ApiResults.MaxJsonBytes"/>; refusing
    /// it lost every iPhone's reports.
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

        if (await ApiResults.ReadAsync<ErrorReportBatch>(request, cancellationToken) is not { } batch)
        {
            return ApiResults.BadRequest();
        }

        return await reports.SaveAsync(batch, cancellationToken)
            ? new NoContentResult()
            : ApiResults.BadRequest();
    }
}
