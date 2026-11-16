using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using SideGigPh.AzureSQL;
using SideGigPh.Contracts;

namespace SideGigPh.Api;

/// <summary>
/// Job posts: a household posts the work it needs done and takes it down again; a worker reads the open posts of
/// their trade within the distance they travel, most relevant first, and can report a fake one. The household's
/// exact point never leaves the server.
/// </summary>
public sealed class JobPostFunctions(FirebaseTokenValidator auth, JobPostStore posts, CallerLimits limits)
{
    /// <summary><c>POST job-posts</c>: the caller's new post, open at once. Answers with it.</summary>
    [Function("AddJobPost")]
    public async Task<IActionResult> Add(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "job-posts")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        if (await auth.GetUidAsync(request, cancellationToken) is not { } uid)
        {
            return new UnauthorizedResult();
        }

        if (!limits.TryTake(Limited.JobPost, uid))
        {
            return ApiResults.TooMany();
        }

        if (await ApiResults.ReadAsync<NewJobPostRequest>(request, cancellationToken) is not { } post)
        {
            return ApiResults.BadRequest();
        }

        var outcome = await posts.AddAsync(uid, post, cancellationToken);
        return outcome.Post is { } saved
            ? new OkObjectResult(saved)
            : ApiResults.BadRequest(ApiErrors.Invalid(outcome.InvalidField!));
    }

    /// <summary><c>GET job-posts/mine</c>: the caller's own posts, newest first.</summary>
    [Function("GetMyJobPosts")]
    public async Task<IActionResult> Mine(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "job-posts/mine")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        if (await auth.GetUidAsync(request, cancellationToken) is not { } uid)
        {
            return new UnauthorizedResult();
        }

        return new OkObjectResult(await posts.MineAsync(uid, cancellationToken));
    }

    /// <summary><c>GET job-posts/feed?skip=0&amp;take=10</c>: one page of the open posts a worker can take, most relevant first.</summary>
    [Function("GetJobPostFeed")]
    public async Task<IActionResult> Feed(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "job-posts/feed")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        if (await auth.GetUidAsync(request, cancellationToken) is not { } uid)
        {
            return new UnauthorizedResult();
        }

        var skip = int.TryParse(request.Query["skip"], out var from) ? from : 0;
        var take = int.TryParse(request.Query["take"], out var count) ? count : Paging.JobPosts;
        return new OkObjectResult(await posts.FeedAsync(uid, skip, take, cancellationToken));
    }

    /// <summary><c>DELETE job-posts/{id}</c>: the caller takes their own post down (it stays in their list as closed).</summary>
    [Function("CloseJobPost")]
    public async Task<IActionResult> Close(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "job-posts/{id}")] HttpRequest request,
        string id,
        CancellationToken cancellationToken)
    {
        if (await auth.GetUidAsync(request, cancellationToken) is not { } uid)
        {
            return new UnauthorizedResult();
        }

        return long.TryParse(id, out var postId) && await posts.CloseAsync(uid, postId, cancellationToken)
            ? new NoContentResult()
            : new NotFoundResult();
    }

    /// <summary><c>POST job-posts/{id}/report</c>: a worker reports a fake or prank post; it joins the operator's report queue.</summary>
    [Function("ReportJobPost")]
    public async Task<IActionResult> Report(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "job-posts/{id}/report")] HttpRequest request,
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

        return long.TryParse(id, out var postId) && await posts.ReportAsync(uid, postId, cancellationToken)
            ? new NoContentResult()
            : new NotFoundResult();
    }
}
