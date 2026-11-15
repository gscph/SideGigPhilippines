using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using SideGigPh.AzureSQL;
using SideGigPh.Contracts;

namespace SideGigPh.Api;

/// <summary>
/// The feedback board: any signed-in person reads it, suggests a feature and votes; the most voted come first.
/// Suggestions carry no names. The caller only ever changes their own suggestion and their own vote.
/// </summary>
public sealed class FeedbackFunctions(FirebaseTokenValidator auth, FeedbackStore feedback, CallerLimits limits)
{
    /// <summary><c>GET feedback?skip=0&amp;take=20</c>: one page of the board, most voted first.</summary>
    [Function("GetFeedback")]
    public async Task<IActionResult> List(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "feedback")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        if (await auth.GetUidAsync(request, cancellationToken) is not { } uid)
        {
            return new UnauthorizedResult();
        }

        var skip = int.TryParse(request.Query["skip"], out var from) ? from : 0;
        var take = int.TryParse(request.Query["take"], out var count) ? count : Paging.Feedback;
        return new OkObjectResult(await feedback.ListAsync(uid, skip, take, cancellationToken));
    }

    /// <summary><c>GET feedback/similar?q=…</c>: suggestions on the board that probably ask for the same thing as <c>q</c>.</summary>
    [Function("GetSimilarFeedback")]
    public async Task<IActionResult> Similar(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "feedback/similar")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        if (await auth.GetUidAsync(request, cancellationToken) is not { } uid)
        {
            return new UnauthorizedResult();
        }

        return new OkObjectResult(await feedback.SimilarAsync(uid, request.Query["q"], cancellationToken));
    }

    /// <summary><c>POST feedback</c>: the caller's new suggestion, with their vote as its first. Answers with it.</summary>
    [Function("AddFeedback")]
    public async Task<IActionResult> Add(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "feedback")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        if (await auth.GetUidAsync(request, cancellationToken) is not { } uid)
        {
            return new UnauthorizedResult();
        }

        if (!limits.TryTake(Limited.Feedback, uid))
        {
            return ApiResults.TooMany();
        }

        if (await ApiResults.ReadAsync<NewFeedbackRequest>(request, cancellationToken) is not { } suggestion)
        {
            return ApiResults.BadRequest();
        }

        return await feedback.AddAsync(uid, suggestion, cancellationToken) is { } post
            ? new OkObjectResult(post)
            : ApiResults.BadRequest(ApiErrors.Invalid("title"));
    }

    /// <summary><c>PUT feedback/{id}/vote</c>: the caller votes for the suggestion. Answers with the new count.</summary>
    [Function("VoteForFeedback")]
    public Task<IActionResult> Vote(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "feedback/{id}/vote")] HttpRequest request,
        string id,
        CancellationToken cancellationToken) => VoteAsync(request, id, on: true, cancellationToken);

    /// <summary><c>DELETE feedback/{id}/vote</c>: the caller takes their vote back. Answers with the new count.</summary>
    [Function("UnvoteFeedback")]
    public Task<IActionResult> Unvote(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "feedback/{id}/vote")] HttpRequest request,
        string id,
        CancellationToken cancellationToken) => VoteAsync(request, id, on: false, cancellationToken);

    /// <summary><c>DELETE feedback/{id}</c>: the caller removes their own suggestion, while nobody else has voted for it.</summary>
    [Function("DeleteFeedback")]
    public async Task<IActionResult> Delete(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "feedback/{id}")] HttpRequest request,
        string id,
        CancellationToken cancellationToken)
    {
        if (await auth.GetUidAsync(request, cancellationToken) is not { } uid)
        {
            return new UnauthorizedResult();
        }

        return long.TryParse(id, out var postId) && await feedback.DeleteAsync(uid, postId, cancellationToken)
            ? new NoContentResult()
            : new NotFoundResult();
    }

    /// <summary><c>POST feedback/{id}/report</c>: the caller reports a suggestion (spam, abuse); it joins the operator's report queue.</summary>
    [Function("ReportFeedback")]
    public async Task<IActionResult> Report(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "feedback/{id}/report")] HttpRequest request,
        string id,
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

        return long.TryParse(id, out var postId) && await feedback.ReportAsync(uid, postId, cancellationToken)
            ? new NoContentResult()
            : new NotFoundResult();
    }

    private async Task<IActionResult> VoteAsync(HttpRequest request, string id, bool on, CancellationToken cancellationToken)
    {
        if (await auth.GetUidAsync(request, cancellationToken) is not { } uid)
        {
            return new UnauthorizedResult();
        }

        if (!limits.TryTake(Limited.FeedbackVote, uid))
        {
            return ApiResults.TooMany();
        }

        return long.TryParse(id, out var postId) && await feedback.VoteAsync(uid, postId, on, cancellationToken) is { } votes
            ? new OkObjectResult(votes)
            : new NotFoundResult();
    }
}
