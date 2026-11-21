using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services;

/// <summary>
/// Everything the Pro home screens share: the pro's saved listing, their photos, chats with
/// customers and reviews, and the actions on them (add photos, publish, reply). One
/// instance for the app, so the top bar and bottom tabs update when a page changes something
/// (<see cref="Changed"/>). Only ports are used here, never adapters.
/// </summary>
public sealed class ProWorkspace(
    IOnboardingStore store,
    IPhotoStore photos,
    IPhotoPicker picker,
    IChatRepository chats,
    IReviewRepository reviews,
    IProfessionalProfileRepository profiles,
    IIdentityProvider identity,
    ISafetyRepository safety)
{
    /// <summary>Most work photos a listing can have.</summary>
    public const int MaxWorkPhotos = 6;

    /// <summary>How long chats and reviews count as fresh: switching tabs within this doesn't ask the server again.</summary>
    public static readonly TimeSpan ActivityFreshFor = TimeSpan.FromSeconds(15);

    private readonly Dictionary<string, string> _photoUrls = [];
    private OnboardingState? _state;
    private Task<bool>? _loading;
    private Task? _activity;
    private DateTimeOffset _refreshedAt = DateTimeOffset.MinValue;
    private bool _publishPrompted;

    /// <summary>
    /// The pro's id in chats and as the listing id: the signed-in user's uid (set when the workspace loads),
    /// or <see cref="ListingMapper.OwnListingId"/> when nobody is signed in.
    /// </summary>
    public string MyId { get; private set; } = ListingMapper.OwnListingId;

    /// <summary>Raised after anything here changes, so the shell can re-render.</summary>
    public event Action? Changed;

    public ProfessionalDraft Draft => _state?.Professional ?? new ProfessionalDraft();

    /// <summary>When the pro finished the interview.</summary>
    public DateTimeOffset? JoinedAt => _state?.ProFinishedAt;

    public bool IsPublished => Draft.PublishedAt is not null;

    /// <summary>The Publish checklist's ticks in this run of the app (never saved; cleared by <see cref="Forget"/>).</summary>
    public PublishCommitments Commitments { get; } = new();

    public string? ProfilePhotoUrl => Url(Draft.ProfilePhotoId);

    /// <summary>Work photos with their image sources, newest first.</summary>
    public IReadOnlyList<WorkPhoto> WorkPhotos =>
        Draft.WorkPhotoIds
            .Select(id => Url(id) is { } url ? new WorkPhoto(id, url) : null)
            .OfType<WorkPhoto>()
            .ToList();

    /// <summary>Chats with customers loaded so far (<see cref="InboxPages"/>), most recent message first.</summary>
    public IReadOnlyList<Chat> Chats { get; private set; } = [];

    /// <summary>The server may have older chats than <see cref="Chats"/>: the Inbox loads them when scrolled to the end.</summary>
    public bool HasMoreChats { get; private set; }

    public IReadOnlyList<Review> Reviews { get; private set; } = [];

    /// <summary>Chats where the customer wrote last and the pro hasn't opened it since: the Inbox badge.</summary>
    public int UnreadCount => UnreadChatIds.Count;

    /// <summary>The unread chats' ids: the app icon's badge and which notifications stay in the tray.</summary>
    public IReadOnlyList<string> UnreadChatIds =>
        Chats.Where(chat => chat.IsUnreadFor(ChatSide.Professional)).Select(chat => chat.Id).ToList();

    /// <summary>Average stars and count, or null with no reviews.</summary>
    public ReviewSummary? ReviewSummary => Reviews.Count == 0 ? null : new ReviewSummary(Reviews.Average(r => r.Rating), Reviews.Count);

    /// <summary>The listing exactly as customers see it, photos included.</summary>
    public ProfessionalListing Listing => Draft.ToListing(MyId, ProfilePhotoUrl, WorkPhotos.Select(photo => photo.Url).ToList());

    /// <summary>
    /// True once <see cref="EnsureLoadedAsync"/> has finished for this person (until <see cref="Forget"/>): the tab
    /// layout coming back from a full-screen page (a chat, a request) shows at once instead of a blank frame.
    /// </summary>
    public bool IsLoaded => _loading is { IsCompletedSuccessfully: true, Result: true } && _activity is { IsCompletedSuccessfully: true };

    /// <summary>
    /// Loads the saved listing and photos, then the pro's chats and reviews, once. False if this user hasn't finished
    /// the Professional interview, in which case the caller sends them back to <c>/</c>.
    /// </summary>
    public async Task<bool> EnsureLoadedAsync()
    {
        if (!await EnsureDraftLoadedAsync())
        {
            return false;
        }

        await (_activity ??= RefreshActivityAsync());
        return true;
    }

    /// <summary>
    /// Loads only what is on the phone (the saved listing, photos, the signed-in account), with no network call, for
    /// the screens that need nothing else (the setup, Publish): they show at once even on a slow connection. False as
    /// in <see cref="EnsureLoadedAsync"/>.
    /// </summary>
    public async Task<bool> EnsureDraftLoadedAsync()
    {
        _loading ??= LoadAsync();
        var loaded = await _loading;
        if (!loaded)
        {
            _loading = null; // Try again once the interview is finished.
        }

        return loaded;
    }

    /// <summary>Re-reads chats and reviews unless they were read within <see cref="ActivityFreshFor"/> (a tab opening).</summary>
    public Task RefreshIfStaleAsync() =>
        DateTimeOffset.UtcNow - _refreshedAt < ActivityFreshFor ? Task.CompletedTask : RefreshActivityAsync();

    /// <summary>Re-reads chats and reviews side by side: they change without the pro doing anything.</summary>
    public async Task RefreshActivityAsync()
    {
        var sync = Draft.ListingOutOfDate ? SyncListingAsync() : Task.FromResult<PublishResult?>(null);
        var inbox = chats.GetInboxAsync(MyId, ChatSide.Professional);
        var about = reviews.GetForProfessionalAsync(MyId);
        var firstPage = await inbox;
        var full = InboxPages.IsFull(firstPage.Count);
        var merged = InboxPages.WithFirstPage(firstPage, full, Chats, chat => chat);
        // Older pages kept below the fresh one keep their own "more" state.
        HasMoreChats = full && (merged.Count == firstPage.Count || HasMoreChats);
        Chats = merged;
        Reviews = await about;
        await sync;
        _refreshedAt = DateTimeOffset.UtcNow;
        Changed?.Invoke();
    }

    /// <summary>The next page of older chats under <see cref="Chats"/> (the Inbox scrolled to its end).</summary>
    public async Task MoreChatsAsync()
    {
        if (!HasMoreChats || Chats.Count == 0)
        {
            return;
        }

        var page = await chats.GetInboxAsync(MyId, ChatSide.Professional, Chats[^1].Id);
        Chats = InboxPages.WithOlder(Chats, page, chat => chat);
        HasMoreChats = InboxPages.IsFull(page.Count);
        Changed?.Invoke();
    }

    public Task<Chat?> GetChatAsync(string chatId) => chats.GetAsync(chatId);

    /// <summary>The chat's newest page of messages, as it opens.</summary>
    public async Task<ChatHistory> GetMessagesAsync(string chatId) => ChatHistory.FromLatest(await chats.GetMessagesAsync(chatId));

    /// <summary><paramref name="shown"/> with the page before its oldest message added above (the worker scrolled to the top).</summary>
    public async Task<ChatHistory> OlderMessagesAsync(string chatId, ChatHistory shown) =>
        shown.HasOlder ? shown.WithOlder(await chats.GetMessagesAsync(chatId, shown.OldestId)) : shown;

    /// <summary><paramref name="shown"/> with the chat's newest messages added under it (a message arrived).</summary>
    public async Task<ChatHistory> NewMessagesAsync(string chatId, ChatHistory shown) => shown.WithLatest(await chats.GetMessagesAsync(chatId));

    /// <summary>
    /// Calls <paramref name="changed"/> with the chat when it gets a message after <paramref name="lastMessageId"/>, until disposed.
    /// Pass the chat on to <see cref="ChatChanged"/>.
    /// </summary>
    public IDisposable WatchChat(string chatId, string? lastMessageId, Action<Chat> changed) => chats.Watch(chatId, lastMessageId, changed);

    /// <summary>A watched chat got a message: its Inbox row shows it, without re-reading the Inbox.</summary>
    public void ChatChanged(Chat chat) => Put(chat);

    /// <summary>The pro answers a customer in their chat. Returns the reply as the server saved it, for the page to show.</summary>
    public async Task<ChatMessage> ReplyAsync(Chat chat, string message)
    {
        var reply = new ChatMessage(Guid.NewGuid().ToString("N"), chat.Id, MyId,
            ChatMessageKind.Text, message.Trim(), DateTimeOffset.Now);
        var saved = await chats.SendAsync(chat, reply);
        Put(saved);
        return saved.LastMessage;
    }

    /// <summary>The pro opened the chat: the customer's messages are read.</summary>
    public async Task MarkReadAsync(string chatId)
    {
        if (Chats.FirstOrDefault(c => c.Id == chatId) is { } chat && chat.IsUnreadFor(ChatSide.Professional))
        {
            await chats.MarkReadAsync(chatId, ChatSide.Professional);
            Put(chat with { ProfessionalReadAt = DateTimeOffset.Now });
        }
    }

    /// <summary>Lets the pro pick their own photo; it replaces the old one.</summary>
    public async Task<PhotoAddResult> ChangeProfilePhotoAsync() => await SetProfilePhotoAsync(await picker.PickAsync(1));

    /// <summary>
    /// Saves a photo the page already got (a selfie: <see cref="IPhotoPicker.TakeSelfieAsync"/>) as the pro's own; it
    /// replaces the old one. Split from taking it so the loading screen covers only the upload, not the camera and its
    /// permission prompt.
    /// </summary>
    public async Task<PhotoAddResult> SetProfilePhotoAsync(PhotoPick pick)
    {
        if (pick.Status != PhotoPickStatus.Picked)
        {
            return PhotoAddResult.From(pick.Status);
        }

        var oldId = Draft.ProfilePhotoId;
        try
        {
            Draft.ProfilePhotoId = await StoreAsync(pick.Photos[0]);
        }
        catch (PhotoStoreException ex)
        {
            return PhotoAddResult.From(ex);
        }

        await SaveAsync(listingChanged: true);
        await DeleteIfUnusedAsync(oldId);
        return new PhotoAddResult(1);
    }

    /// <summary>How many more work photos fit in the gallery.</summary>
    public int WorkPhotoRoom => Math.Max(0, MaxWorkPhotos - Draft.WorkPhotoIds.Count);

    /// <summary>Lets the pro pick work photos.</summary>
    public async Task<PhotoAddResult> AddWorkPhotosAsync() =>
        WorkPhotoRoom > 0 ? await AddWorkPhotosAsync(await picker.PickAsync(WorkPhotoRoom)) : PhotoAddResult.Nothing;

    /// <summary>
    /// Saves work photos the page already picked (at most <see cref="WorkPhotoRoom"/>). Split from picking so the loading screen covers only the upload, not the phone's photo picker.
    /// </summary>
    public async Task<PhotoAddResult> AddWorkPhotosAsync(PhotoPick pick)
    {
        if (pick.Status != PhotoPickStatus.Picked)
        {
            return PhotoAddResult.From(pick.Status);
        }

        var added = 0;
        var problem = PhotoProblem.None;
        foreach (var bytes in pick.Photos)
        {
            string id;
            try
            {
                id = await StoreAsync(bytes);
            }
            catch (PhotoStoreException ex)
            {
                // Keep the photos that did upload; the notice tells the pro some didn't.
                problem = PhotoAddResult.From(ex).Problem;
                break;
            }

            Draft.WorkPhotoIds.Insert(0, id);
            added++;
        }

        if (added > 0)
        {
            await SaveAsync(listingChanged: true);
        }

        return new PhotoAddResult(added, problem);
    }

    /// <summary>Work photos are optional, so any of them can be removed, even the last one of a published listing.</summary>
    public bool CanRemoveWorkPhoto => Draft.WorkPhotoIds.Count > 0;

    public async Task RemoveWorkPhotoAsync(string photoId)
    {
        if (CanRemoveWorkPhoto && Draft.WorkPhotoIds.Remove(photoId))
        {
            await SaveAsync(listingChanged: true);
            await DeleteIfUnusedAsync(photoId);
        }
    }

    /// <summary>The pro ticked every promise and tapped Ready: make the listing searchable, if it has every interview answer and the setup checklist is done.</summary>
    public async Task<PublishResult> PublishAsync()
    {
        if (!Draft.IsListingComplete)
        {
            return PublishResult.Incomplete;
        }

        if (!Draft.HasProof)
        {
            return PublishResult.MissingProof;
        }

        // The phone may have been added to the account since the interview.
        Draft.TakeAccount(await identity.GetCurrentUserAsync());

        var result = await PublishOrCheckAsync();
        if (result == PublishResult.Published)
        {
            Draft.PublishedAt ??= DateTimeOffset.Now;
            Draft.ListingOutOfDate = false;
            await SaveAsync();
        }

        return result;
    }

    // A first publish whose answer was lost on the way back (a 4G drop, a timeout) may have gone live anyway: ask once
    // before saying it failed, so the worker isn't told to try again for a listing customers can already see.
    private async Task<PublishResult> PublishOrCheckAsync()
    {
        try
        {
            return await profiles.PublishAsync(Listing);
        }
        catch (HttpRequestException ex) when (ex.StatusCode is null && !IsPublished)
        {
            profiles.Forget(Listing.Id);
            if (await profiles.GetByIdAsync(Listing.Id) is null)
            {
                throw;
            }

            return PublishResult.Published;
        }
    }

    /// <summary>
    /// Saves where the worker works from: their base point when the phone gave one (null keeps the one they have) and
    /// how far they travel. A live listing is re-published, retried later on failure like any edit.
    /// </summary>
    public async Task SetWorkAreaAsync(GeoPoint? basePoint, int radiusKm)
    {
        if (basePoint is { } point)
        {
            Draft.BaseLatitude = point.Latitude;
            Draft.BaseLongitude = point.Longitude;
        }

        Draft.RadiusKm = WorkArea.ClampRadius(radiusKm);
        await SaveAsync(listingChanged: true);
    }

    /// <summary>
    /// The live listing was edited on the phone (Done on Edit listing): mark it out of date and re-publish it. Answers
    /// what the server said, or null when it wasn't sent or didn't get through (then it is retried later).
    /// </summary>
    public async Task<PublishResult?> ListingEditedAsync()
    {
        if (!IsPublished)
        {
            return null;
        }

        Draft.ListingOutOfDate = true;
        await store.SaveAsync(_state!);
        return await SyncListingAsync();
    }

    /// <summary>
    /// Re-publishes a live listing marked <see cref="ProfessionalDraft.ListingOutOfDate"/>, never throwing: a failure
    /// keeps the mark, and the next refresh of the Pro screens tries again. Not while an edit has left something
    /// missing (the old version stays live). Null when nothing was sent or nobody answered.
    /// </summary>
    public async Task<PublishResult?> SyncListingAsync()
    {
        if (!IsPublished || !Draft.ListingOutOfDate || !Draft.IsListingComplete || !Draft.HasProof)
        {
            return null;
        }

        PublishResult result;
        try
        {
            result = await profiles.PublishAsync(Listing);
        }
        catch (Exception)
        {
            return null; // logged by the adapter; still marked, so retried
        }

        if (result == PublishResult.Published)
        {
            Draft.ListingOutOfDate = false;
            await store.SaveAsync(_state!);
            Changed?.Invoke();
        }

        return result;
    }

    /// <summary>
    /// The listing's number was confirmed with an SMS code: it is the account's verified number (a phone sign-in,
    /// or a number a Google account linked). Always true when the app has no sign-in.
    /// </summary>
    public bool HasVerifiedPhone => identity.Methods == AuthMethods.None || !string.IsNullOrEmpty(Draft.MobileNumber);

    /// <summary>Not live yet and every setup step is done: interview answers, proof and a verified number.</summary>
    public bool IsReadyToPublish => !IsPublished && Draft.IsListingComplete && Draft.HasProof && HasVerifiedPhone;

    /// <summary>
    /// Live, but an Edit listing was left half way (a new trade picked, its jobs not yet): nothing reaches customers
    /// until it's finished (<see cref="SyncListingAsync"/> waits for a complete listing), so Pro home says so.
    /// </summary>
    public bool HasUnfinishedEdit => IsPublished && !Draft.IsListingComplete;

    /// <summary>Where an unfinished edit picks up: the first question it left unanswered.</summary>
    public string UnfinishedEditPath => OnboardingSteps.PathOf(Draft.FirstUnanswered());

    /// <summary>Re-reads the account's name and number: one may have been linked on the add-phone page since loading.</summary>
    public async Task RefreshAccountAsync()
    {
        var hadPhone = HasVerifiedPhone;
        var hadName = Draft.DisplayName;
        Draft.TakeAccount(await identity.GetCurrentUserAsync());
        if (hadName != Draft.DisplayName)
        {
            // Changed on the name page (Me → Change name): customers see the new name. Not awaited, so a page change
            // never waits on the network; a failure stays marked and is retried (SyncListingAsync).
            _ = ListingEditedAsync();
        }

        if (hadPhone != HasVerifiedPhone || hadName != Draft.DisplayName)
        {
            Changed?.Invoke();
        }
    }

    /// <summary>
    /// True once, when the setup checklist has just been finished (<see cref="IsReadyToPublish"/>): the shell then
    /// opens the Publish page so the worker doesn't miss it. Backing out leaves the checklist's Publish button.
    /// </summary>
    public bool TakePublishPrompt()
    {
        if (_publishPrompted || !IsReadyToPublish)
        {
            return false;
        }

        _publishPrompted = true;
        return true;
    }

    /// <summary>Reports a customer from their chat. With <see cref="UserReport.AlsoBlock"/> the chat leaves the Inbox at once.</summary>
    public async Task ReportAsync(UserReport report)
    {
        await safety.ReportAsync(report);
        if (report.AlsoBlock)
        {
            Hide(report.PersonId);
        }
    }

    /// <summary>Blocks a customer: their chat leaves the Inbox, they can't write and won't find this listing.</summary>
    public async Task BlockAsync(string personId)
    {
        await safety.BlockAsync(personId);
        Hide(personId);
    }

    private void Hide(string personId)
    {
        Chats = [.. Chats.Where(c => c.SeekerId != personId)];
        Changed?.Invoke();
    }

    /// <summary>Drops what's held in memory after Start over, so the next load reads fresh.</summary>
    public void Forget()
    {
        _state = null;
        _loading = null;
        _activity = null;
        _refreshedAt = DateTimeOffset.MinValue;
        _publishPrompted = false;
        Commitments.Clear();
        MyId = ListingMapper.OwnListingId;
        _photoUrls.Clear();
        Chats = [];
        HasMoreChats = false;
        Reviews = [];
    }

    /// <summary>
    /// For the interview's selfie question, before the interview is finished: holds the saved answers (the same ones the
    /// interview edits) with the account and photo addresses, so <see cref="SetProfilePhotoAsync"/> works. Nothing is
    /// marked loaded, so Pro home still waits for the finished interview (<see cref="EnsureDraftLoadedAsync"/>).
    /// </summary>
    public async Task LoadInterviewDraftAsync()
    {
        _state = await store.LoadAsync();
        await TakeAccountAndPhotosAsync();
    }

    private async Task<bool> LoadAsync()
    {
        _state = await store.LoadAsync();
        if (!_state.HasFinished(UserRole.Professional))
        {
            return false;
        }

        await TakeAccountAndPhotosAsync();
        return true;
    }

    private async Task TakeAccountAndPhotosAsync()
    {
        var user = await identity.GetCurrentUserAsync();
        MyId = user?.Uid ?? ListingMapper.OwnListingId;
        Draft.TakeAccount(user);

        foreach (var id in Draft.WorkPhotoIds.Append(Draft.ProfilePhotoId).OfType<string>())
        {
            if (await photos.GetUrlAsync(id) is { } url)
            {
                _photoUrls[id] = url;
            }
        }
    }

    /// <summary>Puts the chat at the top of the Inbox (replacing its old row), as the server just answered.</summary>
    private void Put(Chat chat)
    {
        Chats = [chat, .. Chats.Where(c => c.Id != chat.Id)];
        Changed?.Invoke();
    }

    private async Task<string> StoreAsync(byte[] bytes)
    {
        var id = await photos.SaveAsync(bytes);
        _photoUrls[id] = (await photos.GetUrlAsync(id))!;
        return id;
    }

    private async Task DeleteIfUnusedAsync(string? photoId)
    {
        if (photoId is null || photoId == Draft.ProfilePhotoId || Draft.WorkPhotoIds.Contains(photoId))
        {
            return;
        }

        _photoUrls.Remove(photoId);
        await photos.DeleteAsync(photoId);
    }

    private async Task SaveAsync(bool listingChanged = false)
    {
        if (listingChanged && IsPublished)
        {
            Draft.ListingOutOfDate = true;
        }

        await store.SaveAsync(_state!);
        // Keep the live listing in step with new photos; a failure is retried, never thrown at a
        // page after the change was already saved on the phone.
        await SyncListingAsync();
        Changed?.Invoke();
    }

    private string? Url(string? photoId) =>
        photoId is not null && _photoUrls.TryGetValue(photoId, out var url) ? url : null;
}
