using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using SideGigPh.AzureSQL;
using SideGigPh.Contracts;

namespace SideGigPh.Api;

/// <summary>
/// Job posts: a household posts the work it needs done and takes it down again; a worker reads the open posts of
/// their trade within the distance they travel, most relevant first, and can report a fake one. The household's
/// exact point never leaves the server. Offers (inDrive style): a worker takes the budget or names another price;
/// the household reads the offers on its post and picks one, which closes the post.
/// </summary>
public sealed class JobPostFunctions(FirebaseTokenValidator auth, JobPostStore posts, CallerLimits limits, JobAlerts alerts)
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
        if (outcome.Post is not { } saved)
        {
            return ApiResults.BadRequest(ApiErrors.Invalid(outcome.InvalidField!));
        }

        if (outcome.IsNew)
        {
            await alerts.NewPostAsync(saved, cancellationToken);
        }

        return new OkObjectResult(saved);
    }

    /// <summary>
    /// <c>PUT job-posts/{id}</c>: the caller changes their own open post (<see cref="NewJobPostRequest"/>, the whole post).
    /// Answers with it; <c>404</c> when it isn't theirs or is no longer open. Workers aren't alerted again.
    /// </summary>
    [Function("UpdateJobPost")]
    public async Task<IActionResult> Update(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "job-posts/{id}")] HttpRequest request,
        string id,
        CancellationToken cancellationToken)
    {
        if (await auth.GetUidAsync(request, cancellationToken) is not { } uid)
        {
            return new UnauthorizedResult();
        }

        if (!limits.TryTake(Limited.JobPostEdit, uid))
        {
            return ApiResults.TooMany();
        }

        if (!long.TryParse(id, out var postId))
        {
            return new NotFoundResult();
        }

        if (await ApiResults.ReadAsync<NewJobPostRequest>(request, cancellationToken) is not { } post)
        {
            return ApiResults.BadRequest();
        }

        var outcome = await posts.UpdateAsync(uid, postId, post, cancellationToken);
        return outcome switch
        {
            { Post: { } saved } => new OkObjectResult(saved),
            { InvalidField: { } field } => ApiResults.BadRequest(ApiErrors.Invalid(field)),
            _ => new NotFoundResult(),
        };
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

    /// <summary><c>GET offers/mine</c>: the caller's own offers as a worker, newest change first, with how each ended.</summary>
    [Function("GetMyOffers")]
    public async Task<IActionResult> MyOffers(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "offers/mine")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        if (await auth.GetUidAsync(request, cancellationToken) is not { } uid)
        {
            return new UnauthorizedResult();
        }

        return new OkObjectResult(await posts.MyOffersAsync(uid, cancellationToken));
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

    /// <summary>
    /// <c>POST job-posts/{id}/offers</c>: the caller, a worker, takes the budget or names another price
    /// (<see cref="SendOfferRequest"/>); sending again replaces their earlier offer. <c>404</c> when the post isn't
    /// open, is theirs or isn't their trade.
    /// </summary>
    [Function("SendJobOffer")]
    public async Task<IActionResult> SendOffer(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "job-posts/{id}/offers")] HttpRequest request,
        string id,
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

        if (!long.TryParse(id, out var postId))
        {
            return new NotFoundResult();
        }

        if (await ApiResults.ReadAsync<SendOfferRequest>(request, cancellationToken) is not { } offer)
        {
            return ApiResults.BadRequest();
        }

        var sent = await posts.SendOfferAsync(uid, postId, offer, cancellationToken);
        if (sent == OfferOutcome.Sent)
        {
            await alerts.NewOfferAsync(postId, uid, cancellationToken);
        }

        return sent switch
        {
            OfferOutcome.Sent or OfferOutcome.Unchanged => new NoContentResult(),
            OfferOutcome.InvalidPrice => ApiResults.BadRequest(ApiErrors.Invalid("pricePesos")),
            OfferOutcome.InvalidNote => ApiResults.BadRequest(ApiErrors.Invalid("note")),
            _ => new NotFoundResult(),
        };
    }

    /// <summary><c>DELETE job-posts/{id}/offers/mine</c>: the caller takes back their offer on a post still open.</summary>
    [Function("WithdrawJobOffer")]
    public async Task<IActionResult> WithdrawOffer(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "job-posts/{id}/offers/mine")] HttpRequest request,
        string id,
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

        return long.TryParse(id, out var postId) && await posts.WithdrawOfferAsync(uid, postId, cancellationToken)
            ? new NoContentResult()
            : new NotFoundResult();
    }

    /// <summary><c>GET job-posts/{id}/offers</c>: the offers on the caller's own post, each with the worker's listing. <c>404</c> when it isn't theirs.</summary>
    [Function("GetJobOffers")]
    public async Task<IActionResult> Offers(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "job-posts/{id}/offers")] HttpRequest request,
        string id,
        CancellationToken cancellationToken)
    {
        if (await auth.GetUidAsync(request, cancellationToken) is not { } uid)
        {
            return new UnauthorizedResult();
        }

        return long.TryParse(id, out var postId) && await posts.OffersAsync(uid, postId, cancellationToken) is { } offers
            ? new OkObjectResult(offers)
            : new NotFoundResult();
    }

    /// <summary>
    /// <c>GET contacts/{otherUid}</c>: the other person's number once a picked offer links the two, else
    /// <c>{ "phoneNumber": null }</c>. Numbers stay private until a pick.
    /// </summary>
    [Function("GetContact")]
    public async Task<IActionResult> Contact(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "contacts/{otherUid}")] HttpRequest request,
        string otherUid,
        CancellationToken cancellationToken)
    {
        if (await auth.GetUidAsync(request, cancellationToken) is not { } uid)
        {
            return new UnauthorizedResult();
        }

        return new OkObjectResult(new ContactDto(await posts.ContactNumberAsync(uid, otherUid, cancellationToken)));
    }

    /// <summary>
    /// <c>POST job-posts/{id}/offers/{offerId}/pick</c>: the caller picks an offer on their own open post, which
    /// closes it. Answers with the picked offer; <c>404</c> when the post isn't theirs and open or the offer isn't waiting.
    /// </summary>
    [Function("PickJobOffer")]
    public async Task<IActionResult> Pick(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "job-posts/{id}/offers/{offerId}/pick")] HttpRequest request,
        string id,
        string offerId,
        CancellationToken cancellationToken)
    {
        if (await auth.GetUidAsync(request, cancellationToken) is not { } uid)
        {
            return new UnauthorizedResult();
        }

        return long.TryParse(id, out var postId) && long.TryParse(offerId, out var pickedId)
            && await posts.PickAsync(uid, postId, pickedId, cancellationToken) is { } picked
            ? new OkObjectResult(picked)
            : new NotFoundResult();
    }
}
