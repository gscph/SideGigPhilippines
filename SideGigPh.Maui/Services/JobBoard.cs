using SideGigPh.Contracts;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Services;

/// <summary>
/// Job posts as the pages use them. The household's side: their own posts, posting a new one (with photos of the
/// problem uploaded as they are picked) and taking one down. The worker's side: the feed of open posts of their
/// trade within the distance they travel, most relevant first (a service they offer, then nearest, then newest),
/// loaded a page at a time, and reporting a fake one. Offers (inDrive style): a worker takes the budget or names
/// another price, changes it or takes it back; the household reads the offers on a post and picks one, which closes
/// it. Raises <see cref="Changed"/> after each change.
/// </summary>
public sealed class JobBoard(IJobPostRepository posts, IPhotoStore photos)
{
    public const int MinDescription = JobPostRules.MinDescription;
    public const int MaxDescription = JobPostRules.MaxDescription;
    public const int MaxPhotos = JobPostRules.MaxPhotos;
    public const int MaxOfferNote = JobOfferRules.MaxNote;

    private List<JobPost> _mine = [];
    private List<JobPost> _feed = [];

    /// <summary>How many feed posts the server has handed out so far: the next page starts there.</summary>
    private int _feedRead;

    /// <summary>How many pages <see cref="LoadMoreFeedAsync"/> reads in a row while they hold only posts already shown.</summary>
    public const int FeedSkipsAhead = 3;

    /// <summary>Raised after a list changes.</summary>
    public event Action? Changed;

    /// <summary>The household's own posts, newest first, closed ones too.</summary>
    public IReadOnlyList<JobPost> Mine => _mine;

    /// <summary>The worker's feed loaded so far, most relevant first.</summary>
    public IReadOnlyList<JobPost> Feed => _feed;

    /// <summary>More posts follow the loaded part of the feed.</summary>
    public bool HasMore { get; private set; }

    /// <summary>False while the worker has no published listing: the feed stays empty until they publish.</summary>
    public bool HasListing { get; private set; } = true;

    /// <summary>The first field of <paramref name="draft"/> still missing or not valid, or null when it can be posted.</summary>
    public static JobPostField? FirstMissing(JobPostDraft draft) =>
        draft.CategoryId is null ? JobPostField.Trade
        : draft.ServiceId is null || !ReferenceData.ServicesOf(draft.CategoryId).Any(service => service.Id == draft.ServiceId) ? JobPostField.Service
        : !JobPostRules.IsDescription(draft.Description) ? JobPostField.Description
        : !JobPostRules.IsBudgetRange(draft.BudgetMinPesos, draft.BudgetPesos) ? JobPostField.Budget
        : draft.CityId is null ? JobPostField.Town
        : null;

    public static bool CanPost(JobPostDraft draft) => FirstMissing(draft) is null;

    /// <summary>
    /// What to fix before Post works, in a few words (shown under the greyed-out button, so a too-short description or a
    /// small budget doesn't leave the household guessing); null when the draft can be posted.
    /// </summary>
    public static string? MissingHint(JobPostDraft draft, AppText text) => FirstMissing(draft) switch
    {
        JobPostField.Trade => text.Get(Copy.PostMissingTrade),
        JobPostField.Service => text.Get(Copy.PostMissingService),
        JobPostField.Description => text.Format(Copy.PostMissingDescription, MinDescription),
        JobPostField.Budget => text.Format(Copy.PostMissingBudget, Money.Peso(JobPostRules.MinBudgetPesos), Money.Peso(JobPostRules.MaxBudgetPesos)),
        JobPostField.Town => text.Get(Copy.PostMissingTown),
        _ => null,
    };

    /// <summary>True when a worker can send <paramref name="pricePesos"/> with <paramref name="note"/> (no note is fine).</summary>
    public static bool CanOffer(int? pricePesos, string? note) => JobOfferRules.IsPrice(pricePesos) && JobOfferRules.IsNote(note);

    /// <summary>How many more photos fit on <paramref name="draft"/>.</summary>
    public static int PhotoRoom(JobPostDraft draft) => Math.Max(0, MaxPhotos - draft.Photos.Count);

    /// <summary>
    /// The other person's number once a picked offer links the two, for Call; null before that (they chat in the app
    /// until a pick) or when they have none.
    /// </summary>
    public Task<string?> ContactNumberAsync(string otherUid) => posts.GetContactNumberAsync(otherUid);

    /// <summary>The picked jobs between this person and <paramref name="otherUid"/>, newest first (for the chat's job card).</summary>
    public Task<IReadOnlyList<PickedJob>> PickedJobsAsync(string otherUid) => posts.GetPickedJobsAsync(otherUid);

    /// <summary>This person marks the picked job done: the job as it is now, or null when it didn't send.</summary>
    public async Task<PickedJob?> MarkDoneAsync(PickedJob job)
    {
        try
        {
            return await posts.MarkDoneAsync(job.PostId);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return null;
        }
    }

    /// <summary>
    /// A worker rates the household of a completed job (1–5 stars, an optional comment). False when it didn't send or
    /// isn't allowed yet.
    /// </summary>
    public async Task<bool> RateHouseholdAsync(string seekerUid, int rating, string? comment)
    {
        if (rating is < 1 or > 5)
        {
            return false;
        }

        try
        {
            await posts.RateHouseholdAsync(seekerUid, rating, string.IsNullOrWhiteSpace(comment) ? null : comment.Trim());
            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return false;
        }
    }

    /// <summary>Reads the household's own posts again.</summary>
    public async Task LoadMineAsync()
    {
        _mine = [.. await posts.GetMineAsync()];
        Changed?.Invoke();
    }

    /// <summary>
    /// Uploads the photos the page picked and adds them to <paramref name="draft"/> (at most <see cref="PhotoRoom"/>,
    /// skipping one already added). Photos that uploaded before a failure stay.
    /// </summary>
    public async Task<PhotoAddResult> AddPhotosAsync(JobPostDraft draft, PhotoPick pick)
    {
        if (pick.Status != PhotoPickStatus.Picked)
        {
            return PhotoAddResult.From(pick.Status);
        }

        int added = 0, duplicates = 0;
        var problem = PhotoProblem.None;
        foreach (var bytes in pick.Photos.Take(PhotoRoom(draft)))
        {
            try
            {
                var id = await photos.SaveAsync(bytes);
                if (draft.Photos.Any(photo => photo.Id == id))
                {
                    duplicates++;
                    continue;
                }

                draft.Photos.Add(new WorkPhoto(id, (await photos.GetUrlAsync(id))!));
                added++;
            }
            catch (PhotoStoreException ex)
            {
                problem = PhotoAddResult.From(ex).Problem;
                break;
            }
        }

        return new PhotoAddResult(added, duplicates, problem);
    }

    /// <summary>Takes a photo off <paramref name="draft"/> and deletes the stored copy (it was only ever on this draft).</summary>
    public async Task RemovePhotoAsync(JobPostDraft draft, string photoId)
    {
        if (draft.Photos.RemoveAll(photo => photo.Id == photoId) > 0)
        {
            try
            {
                await photos.DeleteAsync(photoId);
            }
            catch (PhotoStoreException)
            {
                // The stored copy stays, unused: nothing shows it.
            }
        }
    }

    /// <summary>Posts <paramref name="draft"/> and puts it at the top of the household's list. Never throws.</summary>
    public async Task<JobPostResult> PostAsync(JobPostDraft draft)
    {
        if (!CanPost(draft))
        {
            return JobPostResult.Incomplete;
        }

        try
        {
            var post = await posts.AddAsync(draft);
            _mine.RemoveAll(p => p.Id == post.Id);
            _mine.Insert(0, post);
            Changed?.Invoke();
            return JobPostResult.Posted;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return JobPostResult.Failed;
        }
    }

    /// <summary>Takes the household's own post down: it stays in their list as closed. False when it didn't go through.</summary>
    public async Task<bool> CloseAsync(JobPost post)
    {
        try
        {
            await posts.CloseAsync(post.Id);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return false;
        }

        var index = _mine.FindIndex(p => p.Id == post.Id);
        if (index >= 0)
        {
            _mine[index] = post with { IsOpen = false };
        }

        Changed?.Invoke();
        return true;
    }

    /// <summary>Reads the worker's feed from the top: the first page replaces whatever was loaded.</summary>
    public async Task LoadFeedAsync()
    {
        var page = await posts.GetFeedAsync(0);
        _feed = [.. page.Posts];
        _feedRead = page.Posts.Count;
        HasMore = page.HasMore;
        HasListing = page.HasListing;
        Changed?.Invoke();
    }

    /// <summary>
    /// Adds the next page under the loaded ones (a post already shown isn't repeated). New posts arriving meanwhile shift
    /// the server's offsets, so a page can hold only posts already shown: it reads on (at most <see cref="FeedSkipsAhead"/>
    /// pages) rather than add nothing, which the list would show as "couldn't load more".
    /// </summary>
    public async Task LoadMoreFeedAsync()
    {
        for (var read = 0; HasMore && read < FeedSkipsAhead; read++)
        {
            var page = await posts.GetFeedAsync(_feedRead);
            _feedRead += page.Posts.Count;
            var shown = _feed.Select(p => p.Id).ToHashSet();
            var fresh = page.Posts.Where(p => !shown.Contains(p.Id)).ToList();
            _feed.AddRange(fresh);
            HasMore = page.HasMore && page.Posts.Count > 0;
            if (fresh.Count > 0 || page.Posts.Count == 0)
            {
                break; // something new, or nothing at all (offline: the list offers Try again)
            }
        }

        Changed?.Invoke();
    }

    /// <summary>Reports a fake or prank post to the operator and takes it off this worker's feed. False when the report didn't send.</summary>
    public async Task<bool> ReportAsync(JobPost post)
    {
        try
        {
            await posts.ReportAsync(post.Id);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return false;
        }

        _feed.RemoveAll(p => p.Id == post.Id);
        Changed?.Invoke();
        return true;
    }

    /// <summary>
    /// The worker sends <paramref name="pricePesos"/> (the budget or their own price) on <paramref name="post"/>, or
    /// changes the price they sent. A post that is no longer open leaves the feed.
    /// </summary>
    public async Task<OfferResult> SendOfferAsync(JobPost post, int pricePesos, string? note)
    {
        if (!CanOffer(pricePesos, note))
        {
            return OfferResult.Failed;
        }

        var result = await WriteAsync(() => posts.SendOfferAsync(post.Id, pricePesos, string.IsNullOrWhiteSpace(note) ? null : note.Trim()));
        UpdateFeed(post.Id, result, p => p with { MyOfferPesos = pricePesos });
        return result;
    }

    /// <summary>The worker takes back their offer on <paramref name="post"/>. A post that is no longer open leaves the feed.</summary>
    public async Task<OfferResult> WithdrawOfferAsync(JobPost post)
    {
        var result = await WriteAsync(() => posts.WithdrawOfferAsync(post.Id));
        UpdateFeed(post.Id, result, p => p with { MyOfferPesos = null });
        return result;
    }

    /// <summary>The offers on the household's own post, the picked one first, then the lowest price. Empty when they can't be read.</summary>
    public Task<IReadOnlyList<JobOffer>> LoadOffersAsync(long postId) => posts.GetOffersAsync(postId);

    /// <summary>
    /// The household picks <paramref name="offer"/>: its post closes and shows who was picked in their list.
    /// </summary>
    public async Task<OfferResult> PickAsync(JobOffer offer)
    {
        var result = await WriteAsync(() => posts.PickOfferAsync(offer.PostId, offer.Id));
        if (result == OfferResult.Done)
        {
            var index = _mine.FindIndex(p => p.Id == offer.PostId);
            if (index >= 0)
            {
                _mine[index] = _mine[index] with { IsOpen = false, PickedProId = offer.Pro.Id };
            }

            Changed?.Invoke();
        }

        return result;
    }

    /// <summary>The household's post, from the loaded list or read again; null when it isn't theirs.</summary>
    public async Task<JobPost?> FindMineAsync(long postId)
    {
        if (!_mine.Any(p => p.Id == postId))
        {
            await LoadMineAsync();
        }

        return _mine.FirstOrDefault(p => p.Id == postId);
    }

    private static async Task<OfferResult> WriteAsync(Func<Task> write)
    {
        try
        {
            await write();
            return OfferResult.Done;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return OfferResult.Gone;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return OfferResult.Failed;
        }
    }

    /// <summary>After an offer write: the post changes when it went through, and leaves the feed when it is gone.</summary>
    private void UpdateFeed(long postId, OfferResult result, Func<JobPost, JobPost> change)
    {
        if (result == OfferResult.Failed)
        {
            return;
        }

        var index = _feed.FindIndex(p => p.Id == postId);
        if (index >= 0)
        {
            if (result == OfferResult.Gone)
            {
                _feed.RemoveAt(index);
            }
            else
            {
                _feed[index] = change(_feed[index]);
            }
        }

        Changed?.Invoke();
    }

    /// <summary>
    /// The worker's own offers (Requests → My offers), newest change first: picked ones open their chat, waiting ones
    /// can still be changed in the feed, and ones not picked say so (before, a worker who wasn't picked just saw the
    /// post leave the feed). Read fresh each time: they change when a household picks.
    /// </summary>
    public Task<IReadOnlyList<MyOffer>> MyOffersAsync() => posts.GetMyOffersAsync();
}

/// <summary>The fields of the post form, in the order they are asked.</summary>
public enum JobPostField
{
    Trade,
    Service,
    Description,
    Budget,
    Town,
}
