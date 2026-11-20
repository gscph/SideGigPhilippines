using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using SideGigPh.Api.Moderation;
using SideGigPh.AzureSQL;
using SideGigPh.Contracts;

namespace SideGigPh.Api;

/// <summary>
/// Picked jobs and verified two-way reviews (brief item 12): the jobs between the caller and one other person, each
/// side marking one done, and a worker's stars for a household after a completed job.
/// </summary>
public sealed class PickedJobFunctions(FirebaseTokenValidator auth, PickedJobStore jobs, CallerLimits limits, TextModeration moderation)
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
    /// <c>PUT household-reviews</c>: the caller, a worker, rates a household after a completed job between them.
    /// <c>403 no-completed-job</c> before that, <c>422 text-not-allowed</c> for a rude comment.
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

        if (!await moderation.AllowsAsync("household review", review.Comment, cancellationToken))
        {
            return ApiResults.TextNotAllowed();
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
