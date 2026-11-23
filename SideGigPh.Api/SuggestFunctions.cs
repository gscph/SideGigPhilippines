using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using SideGigPh.AzureSQL;
using SideGigPh.Contracts;

namespace SideGigPh.Api;

/// <summary>
/// The feedback board as a public web page, the "Feedback URL" Play testers get: the same suggestions as the app's
/// board, most voted first, each with Upvote. Someone with an idea types it first; when the board already has a
/// look-alike (<see cref="FeedbackStore.SimilarAsync"/>) the page offers those to upvote, and only "None of these"
/// posts a new one. No sign-in: a visitor is a random id in a cookie (<see cref="VoterCookie"/>, stored as
/// <c>web-…</c> like a uid), so one browser has one vote per suggestion. Posts and votes are limited per IP address
/// (<see cref="Limited.WebFeedback"/>, <see cref="Limited.WebFeedbackVote"/>) and in total
/// (<see cref="Limited.WebFeedbackTotal"/>); a hidden field bots fill in is ignored. Plain HTML forms, no script.
/// </summary>
public sealed class SuggestFunctions(FeedbackStore feedback, CallerLimits limits)
{
    /// <summary>The cookie holding a visitor's random id.</summary>
    public const string VoterCookie = "tarbaho_voter";

    /// <summary>What a web visitor's id looks like in <c>AuthorUid</c> / <c>VoterUid</c>: <c>web-</c> and 32 hex digits.</summary>
    public const string VoterPrefix = "web-";

    /// <summary>The form field only bots fill in (hidden from people).</summary>
    public const string TrapField = "website";

    /// <summary>The most suggestions the page lists.</summary>
    public const int Shown = 50;

    /// <summary>The largest form the page accepts (a title and details are far smaller).</summary>
    private const long MaxFormBytes = 16 * 1024;

    /// <summary><c>GET suggest</c>: the board, most voted first, and the box to suggest something.</summary>
    [Function("GetSuggestPage")]
    public async Task<IActionResult> Page(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "suggest")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        var voter = Voter(request);
        var board = await feedback.ListAsync(voter, 0, Shown, cancellationToken);
        var notice = request.Query.ContainsKey("posted") ? SuggestPage.Notice.Posted : SuggestPage.Notice.None;
        return SuggestPage.Html(Root(request), board.Posts, new SuggestPage.Draft(), notice);
    }

    /// <summary>
    /// <c>POST suggest</c> (form: <c>title</c>, <c>details</c>, <c>step</c>): first the look-alikes to upvote, if the board
    /// has any; with <c>step=post</c> (or none alike) the suggestion is posted, with the visitor's vote as its first.
    /// </summary>
    [Function("PostSuggestion")]
    public async Task<IActionResult> Post(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "suggest")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        if (await ReadFormAsync(request, cancellationToken) is not { } form)
        {
            return ApiResults.BadRequest();
        }

        var root = Root(request);
        if (!string.IsNullOrEmpty(form[TrapField]))
        {
            return new RedirectResult($"{root}?posted=1");  // a bot: looks done, nothing stored
        }

        var voter = Voter(request);
        var draft = new SuggestPage.Draft(form["title"].ToString().Trim(), form["details"].ToString().Trim());
        if (!FeedbackRules.IsTitle(draft.Title) || draft.Details.Length > FeedbackRules.MaxDetails)
        {
            var board = await feedback.ListAsync(voter, 0, Shown, cancellationToken);
            return SuggestPage.Html(root, board.Posts, draft with { Problem = true }, SuggestPage.Notice.None);
        }

        if (form["step"] != "post")
        {
            var similar = await feedback.SimilarAsync(voter, draft.Title, cancellationToken);
            if (similar.Count > 0)
            {
                var board = await feedback.ListAsync(voter, 0, Shown, cancellationToken);
                return SuggestPage.Html(root, board.Posts, draft with { Similar = similar }, SuggestPage.Notice.None);
            }
        }

        if (!limits.TryTake(Limited.WebFeedback, ClientAddress.Of(request)) || !limits.TryTake(Limited.WebFeedbackTotal, "all"))
        {
            return ApiResults.TooMany();
        }

        var details = draft.Details.Length == 0 ? null : draft.Details;
        return await feedback.AddAsync(voter, new NewFeedbackRequest(draft.Title, details, null), cancellationToken, FeedbackSources.Web) is { } post
            ? new RedirectResult($"{root}?posted=1#post-{post.Id}")
            : ApiResults.BadRequest(ApiErrors.Invalid("title"));
    }

    /// <summary><c>POST suggest/{id}/vote</c> (form: <c>on</c> = 1 or 0): the visitor's upvote added or taken back.</summary>
    [Function("VoteOnSuggestion")]
    public async Task<IActionResult> Vote(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "suggest/{id}/vote")] HttpRequest request,
        string id,
        CancellationToken cancellationToken)
    {
        if (!long.TryParse(id, out var postId) || await ReadFormAsync(request, cancellationToken) is not { } form)
        {
            return new NotFoundResult();
        }

        if (!limits.TryTake(Limited.WebFeedbackVote, ClientAddress.Of(request)))
        {
            return ApiResults.TooMany();
        }

        var voted = await feedback.VoteAsync(Voter(request), postId, on: form["on"] != "0", cancellationToken);
        return voted is null ? new NotFoundResult() : new RedirectResult($"{Root(request)}#post-{postId}");
    }

    /// <summary>The visitor's id from their cookie, or a new one (and the cookie, kept a year).</summary>
    private static string Voter(HttpRequest request)
    {
        if (request.Cookies.TryGetValue(VoterCookie, out var saved) && saved is { Length: 32 } && saved.All(Uri.IsHexDigit))
        {
            return VoterPrefix + saved;
        }

        var id = Guid.NewGuid().ToString("N");
        request.HttpContext.Response.Cookies.Append(VoterCookie, id, new CookieOptions
        {
            HttpOnly = true,
            Secure = request.IsHttps,
            SameSite = SameSiteMode.Lax,
            MaxAge = TimeSpan.FromDays(365),
            Path = Root(request),
        });
        return VoterPrefix + id;
    }

    /// <summary>This page's own path (<c>/api/suggest</c>), whichever of its routes was called.</summary>
    private static string Root(HttpRequest request)
    {
        var path = request.Path.Value ?? "/api/suggest";
        var at = path.IndexOf("/suggest", StringComparison.Ordinal);
        return at < 0 ? "/api/suggest" : path[..(at + "/suggest".Length)];
    }

    private static async Task<IFormCollection?> ReadFormAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        if (!request.HasFormContentType || request.ContentLength is > MaxFormBytes)
        {
            return null;
        }

        try
        {
            return await request.ReadFormAsync(cancellationToken);
        }
        catch (InvalidDataException)
        {
            return null;  // a body past the form limits
        }
    }
}
