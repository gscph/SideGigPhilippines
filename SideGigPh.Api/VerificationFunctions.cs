using ImageMagick;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using SideGigPh.AzureSQL;
using SideGigPh.Contracts;

namespace SideGigPh.Api;

/// <summary>The operators: the accounts allowed on <c>ops/…</c> routes, from the Function App setting <c>AdminUids</c> (uids, comma separated).</summary>
public sealed class AdminList(string? setting)
{
    public const string Setting = "AdminUids";

    private readonly HashSet<string> _uids = [.. (setting ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];

    public bool Contains(string uid) => _uids.Contains(uid);

    /// <summary>The operator's uid, or the answer to give instead: 401 without a sign-in, 403 <c>not-admin</c> for anyone else.</summary>
    public async Task<(string? Admin, IActionResult? Refusal)> CheckAsync(FirebaseTokenValidator auth, HttpRequest request, CancellationToken cancellationToken) =>
        await auth.GetUidAsync(request, cancellationToken) is not { } uid ? (null, new UnauthorizedResult())
        : Contains(uid) ? (uid, null)
        : (null, ApiResults.Forbidden(ApiErrors.NotAdmin));
}

/// <summary>
/// Worker verification: a worker sends a photo of an ID or an NBI or police clearance, with their consent; an operator looks at it and decides; an approved one is a badge on the listing.
/// The photos are sensitive personal information: kept in a private container (<see cref="IPrivateFiles"/>), shown
/// only to operators (each look is logged) and deleted as soon as the operator decides.
/// </summary>
public sealed class VerificationFunctions(
    FirebaseTokenValidator auth, VerificationStore verifications, IPrivateFiles files, IIdDocumentReader reader, AdminList admins, CallerLimits limits)
{
    // What may be sent: an iPhone HEIC can be several MB before it's converted (the stored JPEG is far smaller).
    private const int MaxBytes = 10 * 1024 * 1024;

    /// <summary>
    /// <c>POST verifications/{kind}?consent=true</c>, the body is the photo
    /// (like <c>POST photos</c>). <c>kind</c> is one of <see cref="VerificationKinds"/>; <c>consent=true</c> says the
    /// worker ticked the consent box on the screen that explains the AI scan and how the photo is kept. An ID is read
    /// by AI before it's saved, a clearance's QR code by the server; nothing read comes back to the phone. Every proof gets the photo check
    /// (<see cref="PhotoQuality"/>): what it found comes back as <see cref="VerificationDto.PhotoProblems"/>, only advice,
    /// and <c>&amp;retake=true</c> sends a new photo in place of the caller's own one still waiting for the operator.
    /// </summary>
    [Function("SubmitVerification")]
    public async Task<IActionResult> Submit(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "verifications/{kind}")] HttpRequest request,
        string kind,
        CancellationToken cancellationToken)
    {
        if (await auth.GetUidAsync(request, cancellationToken) is not { } uid)
        {
            return new UnauthorizedResult();
        }

        if (!limits.TryTake(Limited.Verification, uid))
        {
            return ApiResults.TooMany();
        }

        var submission = new VerificationSubmission(
            kind, string.Equals(request.Query["consent"], "true", StringComparison.OrdinalIgnoreCase));
        // A retake replaces the worker's own photo still waiting for the operator, after the photo check said
        // "take it again?" (it is only advice, so the first photo was kept until then).
        // With nothing waiting (a rejected badge sent again), a retake is simply a new request.
        var retake = string.Equals(request.Query["retake"], "true", StringComparison.OrdinalIgnoreCase);
        var refusal = await verifications.RefusalAsync(uid, submission, cancellationToken);
        var replacing = retake && refusal == SubmitOutcome.AlreadySent;
        if (refusal is { } refused && !replacing)
        {
            return Refused(refused);
        }

        var bytes = await PhotoFunctions.ReadAtMostAsync(request.Body, MaxBytes + 1, cancellationToken);
        if (bytes.Length > MaxBytes)
        {
            return new ObjectResult(new ApiError("too-large")) { StatusCode = StatusCodes.Status413PayloadTooLarge };
        }

        if (PhotoImage.Detect(bytes) is not { } format)
        {
            return ApiResults.BadRequest("unsupported-format");
        }

        try
        {
            if (PhotoImage.IsTooBig(bytes, format))
            {
                return new ObjectResult(new ApiError("too-large")) { StatusCode = StatusCodes.Status413PayloadTooLarge };
            }

            bytes = PhotoImage.ToStoredProofJpeg(bytes, format); // upright, no EXIF or GPS, big enough for small print
        }
        catch (MagickException)
        {
            return ApiResults.BadRequest("broken-image");
        }

        var quality = PhotoQuality.Measure(bytes);
        var check = new ProofCheck(quality.Problems, quality.Sharpness);
        var scan = kind switch
        {
            VerificationKinds.Id => await reader.ReadAsync(bytes, cancellationToken),
            VerificationKinds.Clearance => ClearanceQrReader.Read(bytes), // its QR code, only a hint for the operator
            _ => IdScan.None,
        };
        var name = $"{uid}/{Guid.NewGuid():N}.jpg";
        await files.SaveAsync(name, bytes, cancellationToken);
        if (replacing)
        {
            if (await verifications.RetakeAsync(uid, submission, name, scan, check, cancellationToken) is { } replaced)
            {
                if (replaced.OldBlob is not null)
                {
                    await files.DeleteAsync(replaced.OldBlob, cancellationToken);
                }

                return new OkObjectResult(replaced.Request);
            }
        }
        else if (await verifications.SubmitAsync(uid, submission, name, scan, check, cancellationToken) is { } saved)
        {
            return new OkObjectResult(saved);
        }

        await files.DeleteAsync(name, cancellationToken);
        return Refused(SubmitOutcome.AlreadySent);
    }

    /// <summary><c>GET verifications/mine</c>: the caller's own requests and how each stands, newest first.</summary>
    [Function("MyVerifications")]
    public async Task<IActionResult> Mine(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "verifications/mine")] HttpRequest request,
        CancellationToken cancellationToken) =>
        await auth.GetUidAsync(request, cancellationToken) is { } uid
            ? new OkObjectResult(await verifications.MineAsync(uid, cancellationToken))
            : new UnauthorizedResult();

    /// <summary><c>GET ops/me</c>: whether the caller is an operator. No database call: the list is the app setting.</summary>
    [Function("AdminMe")]
    public async Task<IActionResult> AdminMe(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "ops/me")] HttpRequest request,
        CancellationToken cancellationToken) =>
        await auth.GetUidAsync(request, cancellationToken) is { } uid
            ? new OkObjectResult(new AdminStatusDto(admins.Contains(uid)))
            : new UnauthorizedResult();

    /// <summary><c>GET ops/verifications?take=20</c>: the requests waiting for an operator, oldest first.</summary>
    [Function("AdminVerifications")]
    public async Task<IActionResult> Pending(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "ops/verifications")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        var (admin, refusal) = await AdminAsync(request, cancellationToken);
        if (admin is null)
        {
            return refusal!;
        }

        var take = int.TryParse(request.Query["take"], out var asked) ? asked : 20;
        return new OkObjectResult(await verifications.PendingAsync(take, cancellationToken));
    }

    /// <summary><c>GET ops/verifications/{id}/document</c>: the photo, for the operator's eyes. Every look is logged.</summary>
    [Function("AdminVerificationDocument")]
    public async Task<IActionResult> Document(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "ops/verifications/{id}/document")] HttpRequest request,
        string id,
        CancellationToken cancellationToken)
    {
        var (admin, refusal) = await AdminAsync(request, cancellationToken);
        if (admin is null)
        {
            return refusal!;
        }

        if (!long.TryParse(id, out var verificationId)
            || await verifications.ViewDocumentAsync(verificationId, admin, cancellationToken) is not { } name
            || await files.ReadAsync(name, cancellationToken) is not { } bytes)
        {
            return new NotFoundResult();
        }

        request.HttpContext.Response.Headers.CacheControl = "no-store";
        return new FileContentResult(bytes, "image/jpeg");
    }

    /// <summary>
    /// <c>POST ops/verifications/{id}/decision</c>: approve or reject. The photo is deleted either way; only the
    /// result stays (and, for a clearance, the "valid until" the operator typed: 400 <c>valid-until-needed</c> without one).
    /// </summary>
    [Function("AdminVerificationDecision")]
    public async Task<IActionResult> Decide(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "ops/verifications/{id}/decision")] HttpRequest request,
        string id,
        CancellationToken cancellationToken)
    {
        var (admin, refusal) = await AdminAsync(request, cancellationToken);
        if (admin is null)
        {
            return refusal!;
        }

        if (!long.TryParse(id, out var verificationId)
            || await ApiResults.ReadAsync<VerificationDecisionRequest>(request, cancellationToken) is not { } decision)
        {
            return ApiResults.BadRequest();
        }

        if (await verifications.DecideAsync(verificationId, admin, decision, cancellationToken) is not { } decided)
        {
            return new NotFoundResult();
        }

        if (decided.ValidUntilNeeded)
        {
            return ApiResults.BadRequest(ApiErrors.ValidUntilNeeded);
        }

        if (decided.DocumentBlob is { } name)
        {
            await files.DeleteAsync(name, cancellationToken);
        }

        return new NoContentResult();
    }

    private Task<(string? Admin, IActionResult? Refusal)> AdminAsync(HttpRequest request, CancellationToken cancellationToken) =>
        admins.CheckAsync(auth, request, cancellationToken);

    private static IActionResult Refused(SubmitOutcome outcome) => outcome switch
    {
        SubmitOutcome.ConsentRequired => ApiResults.BadRequest(ApiErrors.ConsentRequired),
        SubmitOutcome.AlreadySent => ApiResults.Conflict(ApiErrors.AlreadySent),
        _ => ApiResults.BadRequest(),
    };
}
