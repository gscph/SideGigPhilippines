using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services;

/// <summary>
/// Everything the Pro home screens share: the pro's saved listing, their photos, inquiries
/// and reviews, and the actions on them (add photos, publish, mark inquiries seen). One
/// instance for the app, so the top bar and bottom tabs update when a page changes something
/// (<see cref="Changed"/>). Only ports are used here, never adapters.
/// </summary>
public sealed class ProWorkspace(
    IOnboardingStore store,
    IPhotoStore photos,
    IPhotoPicker picker,
    IInquiryRepository inquiries,
    IReviewRepository reviews,
    IProfessionalProfileRepository profiles)
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

    public IReadOnlyList<Inquiry> Inquiries { get; private set; } = [];

    public IReadOnlyList<Review> Reviews { get; private set; } = [];

    public int NewInquiryCount => Inquiries.Count(IsNew);

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

    /// <summary>Re-reads inquiries and reviews, which change without the pro doing anything.</summary>
    public async Task RefreshActivityAsync()
    {
        Inquiries = await inquiries.GetForProfessionalAsync(ListingMapper.OwnListingId);
        Reviews = await reviews.GetForProfessionalAsync(ListingMapper.OwnListingId);
        Changed?.Invoke();
    }

    public bool IsNew(Inquiry inquiry) => Draft.InquiriesSeenAt is not { } seen || inquiry.At > seen;

    public async Task MarkInquiriesSeenAsync()
    {
        if (NewInquiryCount == 0)
        {
            return;
        }

        Draft.InquiriesSeenAt = DateTimeOffset.Now;
        await SaveAsync();
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
        var result = await profiles.PublishAsync(Listing);
        if (result == PublishResult.Published)
        {
            Draft.PublishedAt ??= DateTimeOffset.Now;
            await SaveAsync();
        }

        return result;
    }

    private async Task<bool> LoadAsync()
    {
        _state = await store.LoadAsync();
        if (_state.Role != UserRole.Professional || _state.CompletedAt is null)
        {
            return false;
        }

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
