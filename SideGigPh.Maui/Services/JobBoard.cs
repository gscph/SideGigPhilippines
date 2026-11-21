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
    public const int MinTitle = JobPostRules.MinTitle;
    public const int MaxTitle = JobPostRules.MaxTitle;
    public const int MaxPhotos = JobPostRules.MaxPhotos;
    public const int MaxOfferNote = JobOfferRules.MaxNote;

    private List<JobPost> _mine = [];
    private List<JobPost> _feed = [];
    private List<MyOffer> _myOffers = [];

    /// <summary>The post form closed with ✕ before Post, kept in memory (this run only) so nothing typed is lost.</summary>
    private JobPostDraft? _unsent;

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
    /// <summary>True when the household typed the problem or added photos: closing the form keeps it.</summary>
    public static bool HasWork(JobPostDraft draft) =>
        !string.IsNullOrWhiteSpace(draft.Title) || !string.IsNullOrWhiteSpace(draft.Description) || draft.Photos.Count > 0;

    /// <summary>The form was closed without posting: keep the draft when it holds the household's own work.</summary>
    public void KeepUnsent(JobPostDraft draft) => _unsent = HasWork(draft) ? draft : null;

    /// <summary>The draft kept by <see cref="KeepUnsent"/>, once (the form opens on it), or null.</summary>
    public JobPostDraft? TakeUnsent()
    {
        var kept = _unsent;
        _unsent = null;
        return kept;
    }

    /// <summary>
    /// Everything still missing before Post works, in the order the form shows it (What, Where, When, Your budget, then
    /// the title and the problem), so the card can mark each empty row and the footer can name them all; empty when it can be posted.
    /// </summary>
    /// <param name="today">The Philippines' date (<see cref="PostDay.Today"/>); null: the phone's clock now.</param>
    public static IReadOnlyList<JobPostField> Missing(JobPostDraft draft, DateOnly? today = null)
    {
        var missing = new List<JobPostField>();
        if (draft.CategoryId is null)
        {
            missing.Add(JobPostField.Trade);
        }
        else if (draft.ServiceId is null || !ReferenceData.ServicesOf(draft.CategoryId).Any(service => service.Id == draft.ServiceId))
        {
            missing.Add(JobPostField.Service);
        }

        if (draft.CityId is null)
        {
            missing.Add(JobPostField.Town);
        }

        if (!PostDay.IsValid(draft.NeededOn, draft.SavedNeededOn, today ?? PostDay.Today(TimeProvider.System)))
        {
            missing.Add(JobPostField.When);
        }

        if (!JobPostRules.IsBudgetRange(draft.BudgetMinPesos, draft.BudgetPesos))
        {
            missing.Add(JobPostField.Budget);
        }

        if (!IsTitleOk(draft.Title))
        {
            missing.Add(JobPostField.Title);
        }

        if (!IsDescriptionOk(draft.Description))
        {
            missing.Add(JobPostField.Description);
        }

        return missing;
    }

    /// <summary>The first thing still missing (where a tap on Post takes the household), or null when it can be posted.</summary>
    public static JobPostField? FirstMissing(JobPostDraft draft, DateOnly? today = null) =>
        Missing(draft, today) is [var first, ..] ? first : null;

    public static bool CanPost(JobPostDraft draft, DateOnly? today = null) => FirstMissing(draft, today) is null;

    /// <summary>
    /// What to do about one missing field, in a few words (on its row of the card, or under the problem's box), so a
    /// too-short description or a small budget doesn't leave the household guessing.
    /// </summary>
    public static string MissingText(JobPostField field, AppText text, JobPostDraft? draft = null) => field switch
    {
        JobPostField.Trade => text.Get(Copy.PostMissingTrade),
        JobPostField.Service => text.Get(Copy.PostMissingService),
        JobPostField.Title when RudeWords.In(draft?.Title) => text.Get(CommonCopy.RudeTextNote),
        JobPostField.Title => text.Format(Copy.PostMissingTitle, MinTitle),
        JobPostField.Description when RudeWords.In(draft?.Description) => text.Get(CommonCopy.RudeTextNote),
        JobPostField.Description => text.Format(Copy.PostMissingDescription, MinDescription),
        JobPostField.Budget => text.Format(Copy.PostMissingBudget, Money.Peso(JobPostRules.MinBudgetPesos), Money.Peso(JobPostRules.MaxBudgetPesos)),
        JobPostField.When => text.Get(Copy.PostMissingWhen),
        _ => text.Get(Copy.PostMissingTown),
    };

    /// <summary>True when a worker can send <paramref name="pricePesos"/> with <paramref name="note"/> (no note is fine).</summary>
    public static bool CanOffer(int? pricePesos, string? note) =>
        JobOfferRules.IsPrice(pricePesos) && JobOfferRules.IsNote(note) && !RudeWords.In(note);

    /// <summary>A title long enough that fits, with no rude words (<see cref="RudeWords"/>): the server refuses the same.</summary>
    public static bool IsTitleOk(string? title) => JobPostRules.IsTitle(title) && !RudeWords.In(title);

    /// <summary>Long enough, and no rude words (<see cref="RudeWords"/>): the server refuses the same.</summary>
    public static bool IsDescriptionOk(string? description) => JobPostRules.IsDescription(description) && !RudeWords.In(description);

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
    /// This person calls off a picked job (<see cref="PickCancel"/>): on the household's side its post is open again in
    /// <see cref="Mine"/> (no pick). <see cref="CancelPickResult.Completed"/> when both had already marked it done.
    /// </summary>
    public async Task<CancelPickResult> CancelPickAsync(PickedJob job, string reason, string? note)
    {
        if (!PickCancel.CanCancel(job) || !PickCancel.IsReady(job.IAmTheHousehold, reason, note))
        {
            return CancelPickResult.Failed;
        }

        try
        {
            await posts.CancelPickAsync(job.PostId, reason, PickCancel.CleanNote(note));
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            return CancelPickResult.Completed;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return CancelPickResult.Failed;
        }

        if (_mine.FindIndex(post => post.Id == job.PostId) is var index and >= 0)
        {
            _mine[index] = _mine[index] with { IsOpen = true, PickedProId = null };
        }

        return CancelPickResult.Cancelled;
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
    /// Uploads the photos the page picked and adds them to <paramref name="draft"/> (at most <see cref="PhotoRoom"/>).
    /// Photos that uploaded before a failure stay.
    /// </summary>
    public async Task<PhotoAddResult> AddPhotosAsync(JobPostDraft draft, PhotoPick pick)
    {
        if (pick.Status != PhotoPickStatus.Picked)
        {
            return PhotoAddResult.From(pick.Status);
        }

        var added = 0;
        var problem = PhotoProblem.None;
        foreach (var bytes in pick.Photos.Take(PhotoRoom(draft)))
        {
            try
            {
                var id = await photos.SaveAsync(bytes);
                draft.Photos.Add(new WorkPhoto(id, (await photos.GetUrlAsync(id))!));
                added++;
            }
            catch (PhotoStoreException ex)
            {
                problem = PhotoAddResult.From(ex).Problem;
                break;
            }
        }

        return new PhotoAddResult(added, problem);
    }

    /// <summary>
    /// Takes a photo off <paramref name="draft"/> and deletes the stored copy when it was only ever on this draft (a photo
    /// of the post being edited stays stored: the post still shows it until Save).
    /// </summary>
    public async Task RemovePhotoAsync(JobPostDraft draft, string photoId)
    {
        if (draft.Photos.RemoveAll(photo => photo.Id == photoId) > 0 && !draft.SavedPhotoIds.Contains(photoId))
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

    /// <summary>
    /// A new post filled in from the household's last search or interview answers: the trade, service, town and
    /// barangay (dropped when no longer offered), the budget range, "Today, it's urgent" as today on the calendar, and
    /// what they typed under "Others" (the "Others" trade or a trade's own "Others" job) as the problem, so most people
    /// only check it and post.
    /// </summary>
    public static JobPostDraft DraftFrom(SeekerAnswers answers, DateOnly today)
    {
        var draft = new JobPostDraft { CategoryId = ReferenceData.FindTrade(answers.CategoryId)?.Id };
        draft.ServiceId = ReferenceData.ServicesOf(draft.CategoryId).FirstOrDefault(service => service.Id == answers.ServiceId)?.Id;
        draft.CityId = ReferenceData.Find(ReferenceData.Cities, answers.TownShown())?.Id;
        draft.BarangayId = ReferenceData.FindBarangay(draft.CityId, answers.BarangayId)?.Id;
        if (answers.IsOtherTrade() || ReferenceData.IsOtherJob(draft.ServiceId))
        {
            draft.Description = answers.OtherJobText;
        }

        if (BudgetRange.TryParse(answers.BudgetId, out var from, out var upTo))
        {
            (draft.BudgetMinPesos, draft.BudgetPesos) = (from > 0 ? from : null, upTo);
        }

        draft.NeededOn = answers.UrgencyId == ReferenceData.UrgencyToday ? today : null;
        return draft;
    }

    /// <summary>
    /// Edit post: a draft holding <paramref name="post"/> as it is now. Its photos are marked saved (taking one off
    /// doesn't delete it until Save), and once workers sent offers its trade and service stay (<see cref="JobPostDraft.WhatLocked"/>).
    /// </summary>
    public static JobPostDraft EditDraft(JobPost post)
    {
        var draft = new JobPostDraft
        {
            EditsPostId = post.Id,
            CategoryId = post.CategoryId,
            ServiceId = post.ServiceId,
            Title = post.Title,
            Description = post.Description,
            BudgetPesos = post.BudgetPesos,
            BudgetMinPesos = post.BudgetMinPesos,
            CityId = post.CityId,
            BarangayId = post.BarangayId,
            NeededOn = post.NeededOn,
            SavedNeededOn = post.NeededOn,
            WhatLocked = post.OfferCount > 0,
        };
        foreach (var (id, url) in (post.PhotoIds ?? []).Zip(post.PhotoUrls))
        {
            draft.Photos.Add(new WorkPhoto(id, url));
            draft.SavedPhotoIds.Add(id);
        }

        return draft;
    }

    /// <summary>
    /// Saves an Edit post draft (<see cref="JobPostDraft.EditsPostId"/>) and puts the changed post in its place in the
    /// household's list. Never throws; <see cref="JobPostResult.Closed"/> when the post is no longer open.
    /// </summary>
    public async Task<JobPostResult> SaveEditAsync(JobPostDraft draft)
    {
        if (draft.EditsPostId is not { } postId || !CanPost(draft))
        {
            return JobPostResult.Incomplete;
        }

        try
        {
            var saved = await posts.UpdateAsync(postId, draft);
            var index = _mine.FindIndex(p => p.Id == postId);
            if (index >= 0)
            {
                // The edit answer doesn't count offers: keep what the list knew.
                _mine[index] = saved with { OfferCount = _mine[index].OfferCount, PickedProId = _mine[index].PickedProId };
            }

            Changed?.Invoke();
            return JobPostResult.Posted;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            await LoadMineAsync();
            return JobPostResult.Closed;
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

        var trimmed = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        var result = await WriteAsync(() => posts.SendOfferAsync(post.Id, pricePesos, trimmed));
        UpdateMyOffers(post.Id, result, offer => offer with { PricePesos = pricePesos, Note = trimmed });
        UpdateFeed(post.Id, result, p => p with { MyOfferPesos = pricePesos });
        return result;
    }

    /// <summary>The worker takes back their offer on <paramref name="post"/>. A post that is no longer open leaves the feed.</summary>
    public async Task<OfferResult> WithdrawOfferAsync(JobPost post)
    {
        var result = await WriteAsync(() => posts.WithdrawOfferAsync(post.Id));
        UpdateMyOffers(post.Id, result, _ => null);
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

    /// <summary>After an offer write: the worker's loaded offer changes (null: taken back) or goes when the post is gone.</summary>
    private void UpdateMyOffers(long postId, OfferResult result, Func<MyOffer, MyOffer?> change)
    {
        var index = _myOffers.FindIndex(o => o.Post.Id == postId);
        if (index < 0 || result == OfferResult.Failed)
        {
            return;
        }

        if (result == OfferResult.Done && change(_myOffers[index]) is { } changed)
        {
            _myOffers[index] = changed;
        }
        else
        {
            _myOffers.RemoveAt(index);
        }
    }

    /// <summary>
    /// The worker's own offers (Requests → My offers), newest change first: picked ones open their chat, waiting ones
    /// can still be changed in the feed, and ones not picked say so (before, a worker who wasn't picked just saw the
    /// post leave the feed). Read fresh each time: they change when a household picks.
    /// </summary>
    public async Task<IReadOnlyList<MyOffer>> MyOffersAsync()
    {
        _myOffers = [.. await posts.GetMyOffersAsync()];
        return _myOffers;
    }

    /// <summary>
    /// One request for its own page, from what is already loaded: the feed first (it is current for open posts), else
    /// the worker's own offers (a picked or closed one). Null when neither holds it.
    /// </summary>
    public RequestItem? FindLoadedRequest(long postId)
    {
        if (_feed.FirstOrDefault(p => p.Id == postId) is { } post)
        {
            var item = RequestItem.From(post);
            return item.State == RequestState.Offered && _myOffers.FirstOrDefault(o => o.Post.Id == postId) is { } offer
                ? item with { MyNote = string.IsNullOrWhiteSpace(offer.Note) ? null : offer.Note }
                : item;
        }

        return _myOffers.FirstOrDefault(o => o.Post.Id == postId) is { } mine ? RequestItem.From(mine) : null;
    }

    /// <summary>
    /// One request for its own page, read again when it isn't loaded (a link from a notification, an app that was
    /// closed): the feed from the top, then the worker's own offers. Null when it is no longer open to this worker.
    /// </summary>
    public async Task<RequestItem?> FindRequestAsync(long postId)
    {
        if (FindLoadedRequest(postId) is { } loaded)
        {
            return loaded;
        }

        await LoadFeedAsync();
        if (FindLoadedRequest(postId) is { } inFeed)
        {
            return inFeed;
        }

        await MyOffersAsync();
        return FindLoadedRequest(postId);
    }
}

/// <summary>The fields of the post form, in the order the form shows them.</summary>
public enum JobPostField
{
    Trade,
    Service,
    Town,

    /// <summary>The picked day has passed (a draft kept overnight): pick another, or any day.</summary>
    When,
    Budget,
    Title,
    Description,
}
