using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using SideGigPh.AzureSQL;
using SideGigPh.Contracts;

namespace SideGigPh.Api;

/// <summary>Reviews: anyone signed in reads them; a Seeker writes one per Skilled Worker they chatted with.</summary>
public sealed class ReviewFunctions(FirebaseTokenValidator auth, ReviewStore reviews)
{
    /// <summary><c>GET reviews?professionalId=…</c> (about a worker) or <c>GET reviews?seekerId=…</c> (written by a Seeker), newest first.</summary>
    [Function("GetReviews")]
    public async Task<IActionResult> Get(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "reviews")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        if (await auth.GetUidAsync(request, cancellationToken) is null)
        {
            return new UnauthorizedResult();
        }

        string? professionalId = request.Query["professionalId"];
        string? seekerId = request.Query["seekerId"];
        if (!string.IsNullOrEmpty(professionalId))
        {
            return new OkObjectResult(await reviews.ForProfessionalAsync(professionalId, cancellationToken));
        }

        return string.IsNullOrEmpty(seekerId)
            ? ApiResults.BadRequest("professionalId-or-seekerId-required")
            : new OkObjectResult(await reviews.BySeekerAsync(seekerId, cancellationToken));
    }

    /// <summary>The caller's review of a worker, replacing their earlier one.</summary>
    [Function("SaveReview")]
    public async Task<IActionResult> Save(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "reviews")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        if (await auth.GetUidAsync(request, cancellationToken) is not { } uid)
        {
            return new UnauthorizedResult();
        }

        if (await ApiResults.ReadAsync<SaveReviewRequest>(request, cancellationToken) is not { } review)
        {
            return ApiResults.BadRequest();
        }

        return await reviews.SaveAsync(uid, review, cancellationToken) switch
        {
            ReviewOutcome.Saved => new NoContentResult(),
            ReviewOutcome.NoChat => ApiResults.Forbidden(ApiErrors.NoChat),
            ReviewOutcome.TooSoon => ApiResults.Forbidden(ApiErrors.ReviewTooSoon),
            _ => ApiResults.BadRequest(),
        };
    }

    /// <summary>Deletes the reviews the caller wrote (Start over). Reviews about them stay.</summary>
    [Function("DeleteMyReviews")]
    public async Task<IActionResult> DeleteMine(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "reviews/mine")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        if (await auth.GetUidAsync(request, cancellationToken) is not { } uid)
        {
            return new UnauthorizedResult();
        }

        await reviews.DeleteWrittenByAsync(uid, cancellationToken);
        return new NoContentResult();
    }
}
