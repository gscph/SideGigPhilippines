namespace SideGigPh.Maui.Models;

/// <summary>
/// One job post: the work a household needs done. A worker sees the town and <paramref name="DistanceKm"/> (whole
/// kilometres from their base point), never the household's exact place.
/// </summary>
/// <param name="PhotoUrls">Photos of the problem, in the order the household added them.</param>
/// <param name="IsOpen">Workers can still see it (the household hasn't taken it down).</param>
/// <param name="Mine">This phone's user posted it.</param>
/// <param name="OffersService">For a worker: their listing has this exact service (else it is only their trade).</param>
/// <param name="DistanceKm">For a worker: how far it is, in whole kilometres; null when unknown or for the household itself.</param>
/// <param name="OfferCount">For the household: how many workers' offers wait on it.</param>
/// <param name="MyOfferPesos">For a worker: the price they offered, or null when they haven't.</param>
/// <param name="PickedProId">For the household: the worker whose offer they picked, else null.</param>
/// <param name="BudgetPesos">The top of the household's budget ("up to"), the price "Take it" offers.</param>
/// <param name="BudgetMinPesos">The low end ("from"), or null when they gave only a top.</param>
/// <param name="BarangayId">The barangay in <paramref name="CityId"/> the household picked, or null (not sure).</param>
public sealed record JobPost(
    long Id,
    string CategoryId,
    string ServiceId,
    string Description,
    int BudgetPesos,
    string CityId,
    IReadOnlyList<string> PhotoUrls,
    bool IsOpen,
    bool Mine,
    bool OffersService,
    int? DistanceKm,
    DateTimeOffset CreatedAt,
    int OfferCount = 0,
    int? MyOfferPesos = null,
    string? PickedProId = null,
    int? BudgetMinPesos = null,
    ReviewSummary? HouseholdStars = null,
    string? BarangayId = null);

/// <summary>
/// One page of a worker's feed, most relevant first; <paramref name="HasMore"/>: another page follows.
/// <paramref name="HasListing"/> is false while the worker has no published listing (the feed is empty then).
/// </summary>
public sealed record JobPostFeed(IReadOnlyList<JobPost> Posts, bool HasMore, bool HasListing);

/// <summary>What a household fills in to post: every field is asked on the post page.</summary>
public sealed class JobPostDraft
{
    public string? CategoryId { get; set; }

    public string? ServiceId { get; set; }

    public string? Description { get; set; }

    /// <summary>The most they are ready to pay ("up to"), in pesos (their own number: Tarbaho sets no prices).</summary>
    public int? BudgetPesos { get; set; }

    /// <summary>The low end of their budget ("from"), in pesos; optional.</summary>
    public int? BudgetMinPesos { get; set; }

    public string? CityId { get; set; }

    /// <summary>The barangay in <see cref="CityId"/> (<c>ReferenceData.BarangaysOf</c>); optional, null = not sure.</summary>
    public string? BarangayId { get; set; }

    /// <summary>
    /// Where the work is, from the phone when the household taps "Use my location" (private: workers see the town and
    /// whole kilometres). Null: the post sits at the centre of <see cref="CityId"/>.
    /// </summary>
    public GeoPoint? Point { get; set; }

    /// <summary>Photos of the problem already uploaded, in the order added.</summary>
    public List<WorkPhoto> Photos { get; } = [];
}

/// <summary>How a worker's own offer stands.</summary>
public enum MyOfferState
{
    /// <summary>The post is open and the household hasn't picked anyone yet.</summary>
    Waiting,

    /// <summary>The household picked this worker: their chat is open.</summary>
    Picked,

    /// <summary>The post closed without picking this worker.</summary>
    NotPicked,
}

/// <summary>
/// One of the worker's own offers: the post, their price and note, how it stands, and once picked the chat the
/// household opened with them.
/// </summary>
public sealed record MyOffer(JobPost Post, int PricePesos, string? Note, MyOfferState State, DateTimeOffset SentAt, string? ChatId);

/// <summary>What became of posting.</summary>
public enum JobPostResult
{
    Posted,

    /// <summary>Something on the form is missing or not valid.</summary>
    Incomplete,

    /// <summary>It didn't reach the server (no internet, server error): worth trying again.</summary>
    Failed,
}

/// <summary>
/// One worker's offer on the household's post (inDrive style): the worker's listing (photo, stars, badges), their
/// price and note, and how far they are from the post in whole kilometres.
/// </summary>
/// <param name="IsPicked">The household picked this one (the post is closed).</param>
/// <param name="TakesBudget">The price is the household's own budget.</param>
public sealed record JobOffer(
    long Id,
    long PostId,
    ProfessionalListing Pro,
    int PricePesos,
    string? Note,
    bool IsPicked,
    bool TakesBudget,
    int? DistanceKm,
    DateTimeOffset CreatedAt);

/// <summary>What became of sending, changing or taking back an offer, or picking one.</summary>
public enum OfferResult
{
    Done,

    /// <summary>The post is no longer open (taken down, another worker picked) or the offer is no longer waiting.</summary>
    Gone,

    /// <summary>It didn't reach the server (no internet, server error): worth trying again.</summary>
    Failed,
}
