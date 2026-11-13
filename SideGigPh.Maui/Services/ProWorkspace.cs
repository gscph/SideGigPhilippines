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
    IIdentityProvider identity)
{
    /// <summary>Most work photos a listing can have.</summary>
    public const int MaxWorkPhotos = 6;

    /// <summary>How long chats and reviews count as fresh: switching tabs within this doesn't ask the server again.</summary>
    public static readonly TimeSpan ActivityFreshFor = TimeSpan.FromSeconds(15);

    private readonly Dictionary<string, string> _photoUrls = [];
    private OnboardingState? _state;
    private Task<bool>? _loading;
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
    public DateTimeOffset? JoinedAt => _state?.CompletedAt;

    public bool IsPublished => Draft.PublishedAt is not null;

    public string? ProfilePhotoUrl => Url(Draft.ProfilePhotoId);

    /// <summary>Work photos with their image sources, newest first.</summary>
    public IReadOnlyList<WorkPhoto> WorkPhotos =>
        Draft.WorkPhotoIds
            .Select(id => Url(id) is { } url ? new WorkPhoto(id, url) : null)
            .OfType<WorkPhoto>()
            .ToList();

    /// <summary>Chats with customers, most recent message first.</summary>
    public IReadOnlyList<Chat> Chats { get; private set; } = [];

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
    /// Loads the saved listing and photos once. False if this user hasn't finished the
    /// Professional interview, in which case the caller sends them back to <c>/</c>.
    /// </summary>
    public async Task<bool> EnsureLoadedAsync()
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
        var inbox = chats.GetInboxAsync(MyId, ChatSide.Professional);
        var about = reviews.GetForProfessionalAsync(MyId);
        Chats = await inbox;
        Reviews = await about;
        _refreshedAt = DateTimeOffset.UtcNow;
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
    public async Task<PhotoAddResult> ChangeProfilePhotoAsync()
    {
        var pick = await picker.PickAsync(1);
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
        return new PhotoAddResult(1, 0);
    }

    /// <summary>Lets the pro pick work photos, skipping any already in their gallery.</summary>
    public async Task<PhotoAddResult> AddWorkPhotosAsync()
    {
        var room = MaxWorkPhotos - Draft.WorkPhotoIds.Count;
        if (room <= 0)
        {
            return PhotoAddResult.Nothing;
        }

        var pick = await picker.PickAsync(room);
        if (pick.Status != PhotoPickStatus.Picked)
        {
            return PhotoAddResult.From(pick.Status);
        }

        int added = 0, duplicates = 0;
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

            if (Draft.WorkPhotoIds.Contains(id))
            {
                duplicates++;
                continue;
            }

            Draft.WorkPhotoIds.Insert(0, id);
            added++;
        }

        if (added > 0)
        {
            await SaveAsync(listingChanged: true);
        }

        return new PhotoAddResult(added, duplicates, problem);
    }

    /// <summary>A published listing keeps at least <see cref="ProfessionalDraft.MinWorkPhotos"/> work photos.</summary>
    public bool CanRemoveWorkPhoto => !IsPublished || Draft.WorkPhotoIds.Count > ProfessionalDraft.MinWorkPhotos;

    public async Task RemoveWorkPhotoAsync(string photoId)
    {
        if (CanRemoveWorkPhoto && Draft.WorkPhotoIds.Remove(photoId))
        {
            await SaveAsync(listingChanged: true);
            await DeleteIfUnusedAsync(photoId);
        }
    }

    /// <summary>
    /// Adds someone who can vouch for the pro. The number is read the way people type it (0917…, +63 917…);
    /// it can't be the pro's own, nor one already on the list.
    /// </summary>
    public async Task<ReferenceAddResult> AddReferenceAsync(string? name, string? typedNumber)
    {
        var trimmed = name?.Trim();
        if (Draft.References.Count >= ProfessionalDraft.MaxReferences)
        {
            return ReferenceAddResult.Full;
        }

        if (string.IsNullOrEmpty(trimmed))
        {
            return ReferenceAddResult.NoName;
        }

        if (!PhilippineMobile.TryNormalize(typedNumber, out var number))
        {
            return ReferenceAddResult.InvalidNumber;
        }

        if (number == Draft.MobileNumber)
        {
            return ReferenceAddResult.OwnNumber;
        }

        if (Draft.References.Any(r => r.MobileNumber == number))
        {
            return ReferenceAddResult.Duplicate;
        }

        Draft.References.Add(new WorkReference(trimmed, number));
        await SaveAsync(listingChanged: true);
        return ReferenceAddResult.Added;
    }

    /// <summary>A published listing keeps at least <see cref="ProfessionalDraft.MinReferences"/> reference.</summary>
    public bool CanRemoveReference => !IsPublished || Draft.References.Count > ProfessionalDraft.MinReferences;

    public async Task RemoveReferenceAsync(WorkReference reference)
    {
        if (CanRemoveReference && Draft.References.Remove(reference))
        {
            await SaveAsync(listingChanged: true);
        }
    }

    /// <summary>The pro took the pledge: make the listing searchable, if it has every interview answer and the setup checklist is done.</summary>
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

        // References added before that couldn't be checked against it.
        if (Draft.References.Any(r => r.MobileNumber == Draft.MobileNumber))
        {
            return PublishResult.BadReference;
        }

        var result = await profiles.PublishAsync(Listing, Draft.References);
        if (result == PublishResult.Published)
        {
            Draft.PublishedAt ??= DateTimeOffset.Now;
            await SaveAsync();
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

    /// <summary>Re-reads the account's name and number: one may have been linked on the add-phone page since loading.</summary>
    public async Task RefreshAccountAsync()
    {
        var hadPhone = HasVerifiedPhone;
        Draft.TakeAccount(await identity.GetCurrentUserAsync());
        if (hadPhone != HasVerifiedPhone)
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

    /// <summary>Drops what's held in memory after Start over, so the next load reads fresh.</summary>
    public void Forget()
    {
        _state = null;
        _loading = null;
        _refreshedAt = DateTimeOffset.MinValue;
        _publishPrompted = false;
        MyId = ListingMapper.OwnListingId;
        _photoUrls.Clear();
        Chats = [];
        Reviews = [];
    }

    private async Task<bool> LoadAsync()
    {
        _state = await store.LoadAsync();
        if (_state.Role != UserRole.Professional || _state.CompletedAt is null)
        {
            return false;
        }

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

        await RefreshActivityAsync();
        return true;
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
        await store.SaveAsync(_state!);
        if (listingChanged && IsPublished && Draft.IsListingComplete && Draft.HasProof)
        {
            // Keep the live listing in step with new photos and references (not while an edit has left something missing).
            await profiles.PublishAsync(Listing, Draft.References);
        }

        Changed?.Invoke();
    }

    private string? Url(string? photoId) =>
        photoId is not null && _photoUrls.TryGetValue(photoId, out var url) ? url : null;
}
