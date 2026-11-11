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
    public const int MaxWorkPhotos = 12;

    private readonly Dictionary<string, string> _photoUrls = [];
    private OnboardingState? _state;
    private Task<bool>? _loading;

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
    public int UnreadCount => Chats.Count(chat => chat.IsUnreadFor(ChatSide.Professional));

    /// <summary>Average stars and count, or null with no reviews.</summary>
    public ReviewSummary? ReviewSummary => Reviews.Count == 0 ? null : new ReviewSummary(Reviews.Average(r => r.Rating), Reviews.Count);

    /// <summary>The listing exactly as customers see it, photos included.</summary>
    public ProfessionalListing Listing => Draft.ToListing(ProfilePhotoUrl, WorkPhotos.Select(photo => photo.Url).ToList());

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

    /// <summary>Re-reads chats and reviews, which change without the pro doing anything.</summary>
    public async Task RefreshActivityAsync()
    {
        Chats = await chats.GetInboxAsync(ListingMapper.OwnListingId, ChatSide.Professional);
        Reviews = await reviews.GetForProfessionalAsync(ListingMapper.OwnListingId);
        Changed?.Invoke();
    }

    public Task<Chat?> GetChatAsync(string chatId) => chats.GetAsync(chatId);

    /// <summary>The chat's messages, oldest first.</summary>
    public Task<IReadOnlyList<ChatMessage>> GetMessagesAsync(string chatId) => chats.GetMessagesAsync(chatId);

    /// <summary>Calls <paramref name="changed"/> when the chat gets a message, until disposed.</summary>
    public IDisposable WatchChat(string chatId, Action changed) => chats.Watch(chatId, changed);

    /// <summary>The pro answers a customer in their chat.</summary>
    public async Task ReplyAsync(Chat chat, string message)
    {
        var reply = new ChatMessage(Guid.NewGuid().ToString("N"), chat.Id, ListingMapper.OwnListingId,
            ChatMessageKind.Text, message.Trim(), DateTimeOffset.Now);
        await chats.SendAsync(chat, reply);
        await RefreshActivityAsync();
    }

    /// <summary>The pro opened the chat: the customer's messages are read.</summary>
    public async Task MarkReadAsync(string chatId)
    {
        if (Chats.FirstOrDefault(c => c.Id == chatId) is { } chat && chat.IsUnreadFor(ChatSide.Professional))
        {
            await chats.MarkReadAsync(chatId, ChatSide.Professional);
            await RefreshActivityAsync();
        }
    }

    /// <summary>Lets the pro pick their own photo; it replaces the old one.</summary>
    public async Task<PhotoAddResult> ChangeProfilePhotoAsync()
    {
        var picked = await picker.PickAsync(1);
        if (picked is null)
        {
            return PhotoAddResult.Failure;
        }

        if (picked.Count == 0)
        {
            return new PhotoAddResult(0, 0, false);
        }

        var oldId = Draft.ProfilePhotoId;
        Draft.ProfilePhotoId = await StoreAsync(picked[0]);
        await SaveAsync(listingChanged: true);
        await DeleteIfUnusedAsync(oldId);
        return new PhotoAddResult(1, 0, false);
    }

    /// <summary>Lets the pro pick work photos, skipping any already in their gallery.</summary>
    public async Task<PhotoAddResult> AddWorkPhotosAsync()
    {
        var room = MaxWorkPhotos - Draft.WorkPhotoIds.Count;
        if (room <= 0)
        {
            return new PhotoAddResult(0, 0, false);
        }

        var picked = await picker.PickAsync(room);
        if (picked is null)
        {
            return PhotoAddResult.Failure;
        }

        int added = 0, duplicates = 0;
        foreach (var bytes in picked)
        {
            var id = await StoreAsync(bytes);
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

        return new PhotoAddResult(added, duplicates, false);
    }

    public async Task RemoveWorkPhotoAsync(string photoId)
    {
        if (Draft.WorkPhotoIds.Remove(photoId))
        {
            await SaveAsync(listingChanged: true);
            await DeleteIfUnusedAsync(photoId);
        }
    }

    /// <summary>The pro took the pledge: make the listing searchable.</summary>
    public async Task<PublishResult> PublishAsync()
    {
        // The phone may have been added to the account since the interview.
        Draft.TakeAccount(await identity.GetCurrentUserAsync());
        var result = await profiles.PublishAsync(Listing);
        if (result == PublishResult.Published)
        {
            Draft.PublishedAt ??= DateTimeOffset.Now;
            await SaveAsync();
        }

        return result;
    }

    /// <summary>Drops what's held in memory after Start over, so the next load reads fresh.</summary>
    public void Forget()
    {
        _state = null;
        _loading = null;
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

        Draft.TakeAccount(await identity.GetCurrentUserAsync());

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
        if (listingChanged && IsPublished)
        {
            // Keep the live listing in step with new photos.
            await profiles.PublishAsync(Listing);
        }

        Changed?.Invoke();
    }

    private string? Url(string? photoId) =>
        photoId is not null && _photoUrls.TryGetValue(photoId, out var url) ? url : null;
}
