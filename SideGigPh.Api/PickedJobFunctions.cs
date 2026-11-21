using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using SideGigPh.AzureSQL;
using SideGigPh.Contracts;

namespace SideGigPh.Api;

/// <summary>
/// Picked jobs and verified two-way reviews (brief item 12): the jobs between the caller and one other person, each
/// side marking one done, and a worker's stars for a household after a completed job.
/// </summary>
public sealed class PickedJobFunctions(FirebaseTokenValidator auth, PickedJobStore jobs, CallerLimits limits)
{
    /// <summary><c>GET picked-jobs/{otherUid}</c>: the picked jobs between the caller and that person, newest first (empty when none).</summary>
    [Function("GetPickedJobs")]
    public async Task<IActionResult> Between(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "picked-jobs/{otherUid}")] HttpRequest request,
        string otherUid,
        CancellationToken cancellationToken)
    {
        if (await auth.GetUidAsync(request, cancellationToken) is not { } uid)
        {
            return new UnauthorizedResult();
        }

        return new OkObjectResult(await jobs.BetweenAsync(uid, otherUid, cancellationToken));
    }

    /// <summary>
    /// <c>POST picked-jobs/{postId}/done</c>: the caller (the household or the picked worker) marks the job done.
    /// Answers with it; <c>404</c> when the caller is neither or nothing was picked there.
    /// </summary>
    [Function("MarkPickedJobDone")]
    public async Task<IActionResult> Done(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "picked-jobs/{postId}/done")] HttpRequest request,
        string postId,
        CancellationToken cancellationToken)
    {
        if (await auth.GetUidAsync(request, cancellationToken) is not { } uid)
        {
            return new UnauthorizedResult();
        }

        if (!limits.TryTake(Limited.JobOffer, uid))
        {
            return ApiResults.TooMany();
        }

        return long.TryParse(postId, out var id) && await jobs.MarkDoneAsync(uid, id, cancellationToken) is { } job
            ? new OkObjectResult(job)
            : new NotFoundResult();
    }

    /// <summary>
    /// <c>POST picked-jobs/{postId}/cancel</c>: the caller (the household or the picked worker) calls the job off with a
    /// reason; the post opens again. <c>204</c>; <c>404</c> when the caller is neither or nothing is picked there;
    /// <c>409 job-completed</c> once both marked it done; <c>400</c> for an unknown reason or a long note.
    /// </summary>
    [Function("CancelPickedJob")]
    public async Task<IActionResult> Cancel(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "picked-jobs/{postId}/cancel")] HttpRequest request,
        string postId,
        CancellationToken cancellationToken)
    {
        if (await auth.GetUidAsync(request, cancellationToken) is not { } uid)
        {
            return new UnauthorizedResult();
        }

        if (!limits.TryTake(Limited.JobOffer, uid))
        {
            return ApiResults.TooMany();
        }

        if (!long.TryParse(postId, out var id) || await ApiResults.ReadAsync<CancelPickRequest>(request, cancellationToken) is not { } cancel)
        {
            return ApiResults.BadRequest();
        }

        return await jobs.CancelAsync(uid, id, cancel, cancellationToken) switch
        {
            CancelPickOutcome.Cancelled => new NoContentResult(),
            CancelPickOutcome.NotFound => new NotFoundResult(),
            CancelPickOutcome.Completed => ApiResults.Conflict(ApiErrors.JobCompleted),
            _ => ApiResults.BadRequest(),
        };
    }

    /// <summary>
    /// <c>PUT picked-jobs/{postId}/location</c>: the caller, the household, sends the worker it picked the exact place
    /// to come to. Answers with the job; <c>404</c> when the caller isn't that post's household or nothing is picked
    /// there; <c>409 job-completed</c> once both marked it done; <c>400</c> for a point outside the Philippines.
    /// </summary>
    [Function("ShareMeetLocation")]
    public async Task<IActionResult> ShareLocation(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "picked-jobs/{postId}/location")] HttpRequest request,
        string postId,
        CancellationToken cancellationToken)
    {
        if (await auth.GetUidAsync(request, cancellationToken) is not { } uid)
        {
            return new UnauthorizedResult();
        }

        if (!limits.TryTake(Limited.JobOffer, uid))
        {
            return ApiResults.TooMany();
        }

        if (!long.TryParse(postId, out var id) || await ApiResults.ReadAsync<ShareMeetLocationRequest>(request, cancellationToken) is not { } share)
        {
            return ApiResults.BadRequest();
        }

        var (outcome, job) = await jobs.ShareLocationAsync(uid, id, share, cancellationToken);
        return outcome switch
        {
            ShareLocationOutcome.Shared when job is not null => new OkObjectResult(job),
            ShareLocationOutcome.NotFound => new NotFoundResult(),
            ShareLocationOutcome.Completed => ApiResults.Conflict(ApiErrors.JobCompleted),
            _ => ApiResults.BadRequest(),
        };
    }

    /// <summary>
    /// <c>PUT household-reviews</c>: the caller, a worker, rates a household after a completed job between them.
    /// <c>403 no-completed-job</c> before that.
    /// </summary>
    [Function("SaveHouseholdReview")]
    public async Task<IActionResult> RateHousehold(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "household-reviews")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        if (await auth.GetUidAsync(request, cancellationToken) is not { } uid)
        {
            return new UnauthorizedResult();
        }

        if (!limits.TryTake(Limited.Review, uid))
        {
            return ApiResults.TooMany();
        }

        if (await ApiResults.ReadAsync<SaveHouseholdReviewRequest>(request, cancellationToken) is not { } review)
        {
            return ApiResults.BadRequest();
        }

        return await jobs.SaveHouseholdReviewAsync(uid, review, cancellationToken) switch
        {
            HouseholdReviewOutcome.Saved => new NoContentResult(),
            HouseholdReviewOutcome.NoCompletedJob => ApiResults.Forbidden(ApiErrors.NoCompletedJob),
            HouseholdReviewOutcome.Locked => ApiResults.Forbidden(ApiErrors.ReviewLocked),
            _ => ApiResults.BadRequest(),
        };
    }
}
