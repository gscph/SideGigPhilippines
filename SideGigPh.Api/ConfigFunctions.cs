using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using SideGigPh.Api.Telemetry;
using SideGigPh.AzureSQL;
using SideGigPh.Contracts;

namespace SideGigPh.Api;

/// <summary>The app's config (answer lists that may be stale), and the "Others" notes people type (analytics).</summary>
public sealed class ConfigFunctions(FirebaseTokenValidator auth, ConfigStore config, OtherJobNoteTable notes, CallerLimits limits)
{
    /// <summary>
    /// <c>GET config</c>: the trades and jobs, towns, price tiers, experience, urgency and income goals. Public reference
    /// data, so no sign-in is needed (the app loads it when it opens, before the account is ready). Cached
    /// <see cref="ConfigStore.ConfigFor"/> here and, through <c>Cache-Control</c>, by any HTTP cache on the way. Each answer
    /// carries its version as an <c>ETag</c>; a phone that sends it back (<c>If-None-Match</c>) and still has the latest
    /// gets <c>304 Not Modified</c> with no body.
    /// </summary>
    [Function("GetConfig")]
    public async Task<IActionResult> Get(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "config")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        var snapshot = await config.GetAsync(cancellationToken);
        var headers = request.HttpContext.Response.Headers;
        headers.ETag = snapshot.Version;
        headers.CacheControl = $"public, max-age={(int)ConfigStore.ConfigFor.TotalSeconds}";

        return request.Headers.IfNoneMatch.Any(tags => tags?.Split(',').Any(tag => tag.Trim() == snapshot.Version) == true)
            ? new StatusCodeResult(StatusCodes.Status304NotModified)
            : new OkObjectResult(snapshot.Config);
    }

    /// <summary><c>POST other-jobs</c>: what the caller typed after picking "Others" in a trade.</summary>
    [Function("NoteOtherJob")]
    public async Task<IActionResult> NoteOtherJob(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "other-jobs")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        if (await auth.GetUidAsync(request, cancellationToken) is not { } uid)
        {
            return new UnauthorizedResult();
        }

        if (!limits.TryTake(Limited.OtherJobNote, uid))
        {
            return ApiResults.TooMany();
        }

        if (await ApiResults.ReadAsync<OtherJobRequest>(request, cancellationToken) is not { } note)
        {
            return ApiResults.BadRequest();
        }

        return await notes.SaveAsync(note, cancellationToken)
            ? new NoContentResult()
            : ApiResults.BadRequest();
    }
}
