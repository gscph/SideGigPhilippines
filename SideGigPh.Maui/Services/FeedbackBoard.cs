using SideGigPh.Contracts;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services.Analytics;

namespace SideGigPh.Maui.Services;

/// <summary>
/// The feedback board as the pages use it (both sides share it): the suggestions loaded so far, most voted first,
/// and what a person can do with them: load more, look for one like theirs before posting, post, vote or take the
/// vote back, remove their own, report someone else's. Raises <see cref="Changed"/> after each change. A vote shows
/// at once and is put back if it didn't reach the server. The list keeps its order while it is on screen (a row
/// doesn't jump away under the finger); the next <see cref="LoadAsync"/> sorts it again.
/// </summary>
public sealed class FeedbackBoard(IFeedbackRepository feedback, IOnboardingStore store, AppAnalytics analytics)
{
    /// <summary>The longest title and details the board takes (the server's limits).</summary>
    public const int MaxTitle = FeedbackRules.MaxTitle;
    public const int MaxDetails = FeedbackRules.MaxDetails;

    private readonly HashSet<long> _voting = [];
    private List<FeedbackPost> _posts = [];

    /// <summary>Raised after the list, a vote or a count changes.</summary>
    public event Action? Changed;

    /// <summary>The suggestions loaded so far, in the board's order.</summary>
    public IReadOnlyList<FeedbackPost> Posts => _posts;

    /// <summary>More suggestions follow the loaded ones.</summary>
    public bool HasMore { get; private set; }

    /// <summary>The first page has been read (even when it came back empty).</summary>
    public bool IsLoaded { get; private set; }

    /// <summary>True when <paramref name="title"/> says enough to post (not just a word or two letters) and fits.</summary>
    public static bool CanPost(string? title) => FeedbackRules.IsTitle(title);

    /// <summary>Reads the board from the top: the first page replaces whatever was loaded.</summary>
    public async Task LoadAsync()
    {
        var page = await feedback.GetPageAsync(0);
        _posts = [.. page.Posts];
        HasMore = page.HasMore;
        IsLoaded = true;
        Changed?.Invoke();
    }

    /// <summary>Adds the next page under the loaded ones (a suggestion already shown isn't repeated).</summary>
    public async Task LoadMoreAsync()
    {
        if (!HasMore)
        {
            return;
        }

        var page = await feedback.GetPageAsync(_posts.Count);
        var shown = _posts.Select(p => p.Id).ToHashSet();
        _posts.AddRange(page.Posts.Where(p => !shown.Contains(p.Id)));
        HasMore = page.HasMore && page.Posts.Count > 0;
        Changed?.Invoke();
    }

    /// <summary>Suggestions already on the board that probably ask for the same thing, closest first.</summary>
    public Task<IReadOnlyList<FeedbackPost>> FindSimilarAsync(string title) => feedback.FindSimilarAsync(title.Trim());

    /// <summary>
    /// Posts a suggestion and puts it at the top of the list, where its writer sees it. Throws when it didn't send:
    /// <see cref="TextNotAllowedException"/> when the server refused its words (nothing posted, the list unchanged).
    /// </summary>
    public async Task<FeedbackPost> PostAsync(string title, string? details)
    {
        var role = (await store.LoadAsync()).Role;
        var post = await feedback.AddAsync(title.Trim(), string.IsNullOrWhiteSpace(details) ? null : details.Trim(), role);
        _posts.RemoveAll(p => p.Id == post.Id);
        _posts.Insert(0, post);
        analytics.Track(AppEvents.FeedbackPosted);
        Changed?.Invoke();
        return post;
    }

    /// <summary>Votes for <paramref name="post"/>, or takes the vote back when it already has this person's. False when it didn't go through (the row is put back).</summary>
    public Task<bool> ToggleVoteAsync(FeedbackPost post) => VoteAsync(post, !post.VotedByMe);

    /// <summary>
    /// Adds or takes back this person's vote. The row changes at once and is corrected by the server's count, or put
    /// back when the vote didn't go through (false). A second tap while one is on its way is ignored.
    /// </summary>
    public async Task<bool> VoteAsync(FeedbackPost post, bool on)
    {
        if (post.VotedByMe == on)
        {
            return true;
        }

        if (!_voting.Add(post.Id))
        {
            return false;
        }

        try
        {
            Put(post with { VotedByMe = on, Votes = Math.Max(0, post.Votes + (on ? 1 : -1)) });
            var votes = await feedback.VoteAsync(post.Id, on);
            Put(post with { VotedByMe = on, Votes = votes });
            if (on)
            {
                analytics.Track(AppEvents.FeedbackVoted);
            }

            return true;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            Put(post);
            return false;
        }
        finally
        {
            _voting.Remove(post.Id);
        }
    }

    /// <summary>Removes this person's own suggestion. False when it wasn't removed (someone else voted meanwhile, or no connection).</summary>
    public Task<bool> RemoveAsync(FeedbackPost post) => TakeOffAsync(post, () => feedback.RemoveAsync(post.Id));

    /// <summary>Reports a suggestion to the operator and takes it off this person's list. False when the report didn't send.</summary>
    public Task<bool> ReportAsync(FeedbackPost post) => TakeOffAsync(post, () => feedback.ReportAsync(post.Id));

    private async Task<bool> TakeOffAsync(FeedbackPost post, Func<Task> send)
    {
        try
        {
            await send();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return false;
        }

        _posts.RemoveAll(p => p.Id == post.Id);
        Changed?.Invoke();
        return true;
    }

    /// <summary>Shows <paramref name="post"/> in place of the loaded one with its id (nothing when it isn't loaded).</summary>
    private void Put(FeedbackPost post)
    {
        var index = _posts.FindIndex(p => p.Id == post.Id);
        if (index >= 0)
        {
            _posts[index] = post;
        }

        Changed?.Invoke();
    }
}
