using System.Text.Json.Serialization;

namespace SideGigPh.Maui.Models;

/// <summary>
/// Everything the onboarding interview has collected, saved on the device with no account.
/// Ids refer to entries in <c>ReferenceData</c>.
/// </summary>
public sealed class OnboardingState
{
    public AppLanguage? Language { get; set; }

    /// <summary>
    /// The side the app opens on. One account can be both: the other side's answers are kept, and the Me page switches
    /// (<c>LocalAccount.SwitchSideAsync</c>).
    /// </summary>
    public UserRole? Role { get; set; }

    /// <summary>The interview step to reopen if the app is closed mid-interview (of the <see cref="Role"/> side).</summary>
    public string? ResumePath { get; set; }

    public SeekerAnswers Seeker { get; set; } = new();

    public ProfessionalDraft Professional { get; set; } = new();

    /// <summary>Set when the user finishes the household interview; kept when they switch to the worker side.</summary>
    public DateTimeOffset? SeekerFinishedAt { get; set; }

    /// <summary>Set when the user finishes the worker interview; kept when they switch to the household side.</summary>
    public DateTimeOffset? ProFinishedAt { get; set; }

    /// <summary>
    /// Only in files saved before one account could be both sides: when the then-current side's interview was finished.
    /// <c>OnboardingCleanup.MoveOldFinishDate</c> moves it to that side's date as the file loads; never written again.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>When that side's interview was finished, or null while it isn't.</summary>
    public DateTimeOffset? FinishedAt(UserRole role) => role == UserRole.Seeker ? SeekerFinishedAt : ProFinishedAt;

    /// <summary>That side's interview is finished: the app opens its home, not its questions.</summary>
    public bool HasFinished(UserRole role) => FinishedAt(role) is not null;
}

/// <summary>A household's answers: what job, where, when, for how much, and whether to search or post.</summary>
public sealed class SeekerAnswers
{
    public string? CategoryId { get; set; }

    public string? ServiceId { get; set; }

    /// <summary>What they typed after picking the trade's "Others" job (sent for analytics; also words their first message).</summary>
    public string? OtherJobText { get; set; }

    /// <summary>The area of their home town (<c>ReferenceData.AreaOf</c>), never asked: set with <see cref="CityId"/>.</summary>
    public string? AreaId { get; set; }

    /// <summary>Their one home town (<c>ReferenceData.Cities</c>).</summary>
    public string? CityId { get; set; }

    /// <summary>
    /// Their barangay in <see cref="CityId"/> (<c>ReferenceData.BarangaysOf</c>), optional: picked on the town question or
    /// a post, and filled into their next post. Dropped when it is retired or not in their town (<c>OnboardingCleanup</c>).
    /// </summary>
    public string? BarangayId { get; set; }

    public string? UrgencyId { get; set; }

    /// <summary>
    /// A peso range (<c>BudgetRange</c>: <c>"800-1500"</c>, <c>"1500-up"</c>), or <c>ReferenceData.Unsure</c>.
    /// </summary>
    public string? BudgetId { get; set; }

    /// <summary>How they order search results (best match or lowest price first), kept like the rest of the search.</summary>
    public SearchOrder SearchOrder { get; set; }

    /// <summary>
    /// How they want to find a worker, asked last (<c>ReferenceData.FindByChoices</c>): <c>"search"</c> opens the results,
    /// <c>"post"</c> the form to post what they need. Null for someone who finished the interview before it was asked.
    /// </summary>
    public string? FindBy { get; set; }

    /// <summary>
    /// Their last searches and posts, newest first, at most <c>RecentSearches.Max</c>, kept only on the phone
    /// (<c>Services/RecentSearches</c>). Seeker home lists them for a quick search or post.
    /// </summary>
    public List<RecentSearch> RecentSearches { get; set; } = [];

    /// <summary>What they last typed in the search box, shown again when they come back.</summary>
    public string? SearchText { get; set; }

    /// <summary>The name they put on their reviews, e.g. "Maricel D.".</summary>
    public string? ReviewerName { get; set; }

    /// <summary>Their one profile photo (SHA-256 id). A new one replaces it and the old one is deleted.</summary>
    public string? ProfilePhotoId { get; set; }
}

/// <summary>A Professional's listing: built in the interview, then photos and publishing on Pro home.</summary>
public sealed class ProfessionalDraft
{
    /// <summary>The one trade the Professional works in. A pro picks a single category.</summary>
    public string? CategoryId { get; set; }

    public List<string> ServiceIds { get; set; } = [];

    /// <summary>What they typed when they ticked the trade's "Others" job (sent for analytics, never shown).</summary>
    public string? OtherJobText { get; set; }

    /// <summary>The one area they work in (<c>ReferenceData.AreaOf</c> their towns), never asked: set with <see cref="CityIds"/>.</summary>
    public string? AreaId { get; set; }

    /// <summary>The towns they go to (<c>ReferenceData.Cities</c>), several allowed.</summary>
    public List<string> CityIds { get; set; } = [];

    public string? ExperienceId { get; set; }

    /// <summary>Their average price per service, in pesos (<c>WorkerPrice</c>): only a guide for customers.</summary>
    public int? RatePesos { get; set; }

    public string? DisplayName { get; set; }

    /// <summary>E.164 format, e.g. +639171234567.</summary>
    public string? MobileNumber { get; set; }

    /// <summary>The pro's own photo, an <c>IPhotoStore</c> id.</summary>
    public string? ProfilePhotoId { get; set; }

    /// <summary>
    /// The account (uid) that sent <see cref="ProfilePhotoId"/> as its selfie; null when unknown (a listing brought back
    /// from the server). The server takes a new profile photo only from the account that took it, so another account
    /// signed in on this phone (<c>IAppAccount.AccountSwitchedAsync</c>, a sign-out and sign-in) drops it and takes its
    /// own (<c>ListingMapper.TakeAccount</c>).
    /// </summary>
    public string? ProfilePhotoOwnerId { get; set; }

    /// <summary>Photos of their work, <c>IPhotoStore</c> ids, newest first.</summary>
    public List<string> WorkPhotoIds { get; set; } = [];

    /// <summary>
    /// The worker's base point, read from the phone when they tap "Use my location" (private: only the server gets it,
    /// and others see a town or whole kilometres). Null until then: the server uses the middle of their towns.
    /// </summary>
    public double? BaseLatitude { get; set; }

    public double? BaseLongitude { get; set; }

    /// <summary>How far they travel, in km (<c>WorkArea.RadiusChoices</c>); null until they pick (the server starts at 10).</summary>
    public int? RadiusKm { get; set; }

    public GeoPoint? BasePoint =>
        BaseLatitude is { } latitude && BaseLongitude is { } longitude ? new GeoPoint(latitude, longitude) : null;

    /// <summary>Set when the pro ticked the Publish promises and published the listing.</summary>
    public DateTimeOffset? PublishedAt { get; set; }

    /// <summary>
    /// The live listing is behind this phone's copy: an edit (Edit listing, photos) didn't reach the
    /// server yet (weak signal, cold server, refused). Cleared when a re-publish goes through; until then the Pro
    /// screens try again (<c>ProWorkspace.SyncListingAsync</c>), so an edit is never silently lost.
    /// </summary>
    public bool ListingOutOfDate { get; set; }

    /// <summary>
    /// Has every answer the listing shows: trade, at least one job, area, at least one town, experience and price.
    /// Only then can it be published (the API refuses a listing without trade, area, jobs and towns).
    /// </summary>
    public bool IsListingComplete =>
        CategoryId is not null && ServiceIds.Count > 0
        && AreaId is not null && CityIds.Count > 0
        && ExperienceId is not null && RatePesos is not null;

    public bool HasProfilePhoto => ProfilePhotoId is not null;

    /// <summary>
    /// The setup checklist is done: own photo (the API refuses a listing without it). Together with
    /// <see cref="IsListingComplete"/> the listing can be published. Work photos are optional: they never block
    /// publishing, they only help the listing be seen (a search bonus, <see cref="GoodWorkPhotos"/>).
    /// </summary>
    public bool HasProof => HasProfilePhoto;

    /// <summary>
    /// Work photos that earn the full search bonus (the API's <c>SearchRanker.FullPhotoBonusAt</c>); fewer still help.
    /// A guide for the "be seen more" item, never a requirement.
    /// </summary>
    public const int GoodWorkPhotos = 2;
}
