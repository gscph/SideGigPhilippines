namespace SideGigPh.Contracts;

/// <summary>
/// One job post: the work a household needs done, as the caller sees it. The household's exact point is never
/// sent: a worker gets the town and <paramref name="DistanceKm"/>, rounded to a whole kilometre.
/// </summary>
/// <param name="Status">One of <see cref="JobPostStatuses"/>.</param>
/// <param name="Photos">Photos of the problem, in the order the household added them.</param>
/// <param name="Mine">The caller posted it.</param>
/// <param name="OffersService">The caller is a worker whose listing has this exact service (else: same trade only).</param>
/// <param name="DistanceKm">For a worker: how far it is from their base point, in whole kilometres; null when unknown or for the household itself.</param>
/// <param name="OfferCount">For the household: how many workers' offers wait on it (withdrawn ones not counted).</param>
/// <param name="MyOfferPesos">For a worker: the price they offered on it, or null when they haven't (or took it back).</param>
/// <param name="PickedProId">For the household: the worker whose offer they picked (the post is closed then), else null.</param>
public sealed record JobPostDto(
    long Id,
    string CategoryId,
    string ServiceId,
    string Description,
    int BudgetPesos,
    string CityId,
    string Status,
    List<PhotoDto> Photos,
    bool Mine,
    bool OffersService,
    int? DistanceKm,
    DateTimeOffset CreatedAt,
    int OfferCount = 0,
    int? MyOfferPesos = null,
    string? PickedProId = null,
    int? BudgetMinPesos = null);

/// <summary>
/// <c>POST job-posts</c>: a household's new post. <paramref name="Latitude"/> / <paramref name="Longitude"/> are the
/// phone's point when it has one; without them the post sits at the centre of <paramref name="CityId"/>.
/// </summary>
public sealed record NewJobPostRequest(
    string CategoryId,
    string ServiceId,
    string Description,
    int BudgetPesos,
    string CityId,
    List<string>? PhotoIds = null,
    double? Latitude = null,
    double? Longitude = null,
    int? BudgetMinPesos = null);

/// <summary>
/// <c>GET job-posts/feed</c>: one page of the open posts a worker can take: their own trade, within the distance they
/// travel, posts for a service they offer first, then nearest, then newest. <paramref name="HasListing"/> is false
/// while the worker has no published listing (the feed is empty then: the server doesn't know their trade yet).
/// </summary>
public sealed record JobPostFeedDto(List<JobPostDto> Posts, bool HasMore, bool HasListing);

/// <summary>Where a job post stands.</summary>
public static class JobPostStatuses
{
    /// <summary>Workers of that trade nearby can see it.</summary>
    public const string Open = "open";

    /// <summary>The household took it down (found someone, or no longer needs it).</summary>
    public const string Closed = "closed";

    /// <summary>The operator hid it after a report. Nobody but the operator sees it.</summary>
    public const string Hidden = "hidden";
}

/// <summary>The limits both sides keep for a job post (the columns of <c>JobPosts</c>).</summary>
public static class JobPostRules
{
    /// <summary>The shortest description that says something ("Tulo ang gripo").</summary>
    public const int MinDescription = 10;
    public const int MaxDescription = 600;

    public const int MinBudgetPesos = 50;
    public const int MaxBudgetPesos = 1_000_000;

    /// <summary>The most photos of the problem one post carries.</summary>
    public const int MaxPhotos = 3;

    /// <summary>True when <paramref name="description"/>, trimmed, says enough and fits.</summary>
    public static bool IsDescription(string? description) =>
        description?.Trim() is { Length: >= MinDescription and <= MaxDescription };

    public static bool IsBudget(int? pesos) => pesos is >= MinBudgetPesos and <= MaxBudgetPesos;

    /// <summary>A budget "from" (optional) and "up to": the top is a budget, and the low end, when given, is 0 to the top.</summary>
    public static bool IsBudgetRange(int? from, int? upTo) => IsBudget(upTo) && (from is null || (from >= 0 && from <= upTo));
}

/// <summary>
/// One worker's offer on a household's post, as the household sees it (<c>GET job-posts/{id}/offers</c>): the
/// worker's published listing (photo, stars, badges), their price and note, and how far they are from the post in
/// whole kilometres.
/// </summary>
/// <param name="Status">One of <see cref="JobOfferStatuses"/>.</param>
/// <param name="TakesBudget">The price is the household's own budget.</param>
public sealed record JobOfferDto(
    long Id,
    long PostId,
    ListingDto Pro,
    int PricePesos,
    string? Note,
    string Status,
    bool TakesBudget,
    int? DistanceKm,
    DateTimeOffset CreatedAt);

/// <summary>
/// <c>POST job-posts/{id}/offers</c>: a worker takes the post's budget (send it as <paramref name="PricePesos"/>) or
/// names another price, with an optional short note. Sending again replaces their earlier offer.
/// </summary>
public sealed record SendOfferRequest(int PricePesos, string? Note = null);

/// <summary>
/// One of the caller's own offers (<c>GET offers/mine</c>), as the worker sees it: the post (as in their feed, its
/// <see cref="JobPostDto.MyOfferPesos"/> set), their price and note, and how it ended. <paramref name="ChatId"/> is
/// set once the household picked it (the chat the household opened with them).
/// </summary>
/// <param name="State">One of <see cref="MyOfferStates"/>.</param>
public sealed record MyOfferDto(
    JobPostDto Post,
    int PricePesos,
    string? Note,
    string State,
    DateTimeOffset SentAt,
    string? ChatId = null);

/// <summary>How a worker's own offer stands (<see cref="MyOfferDto.State"/>).</summary>
public static class MyOfferStates
{
    /// <summary>The post is open and the household hasn't picked anyone yet.</summary>
    public const string Waiting = "waiting";

    /// <summary>The household picked this worker.</summary>
    public const string Picked = "picked";

    /// <summary>The post was closed without picking this worker (another worker, or the household took it down).</summary>
    public const string NotPicked = "not-picked";
}

/// <summary>Where an offer stands.</summary>
public static class JobOfferStatuses
{
    /// <summary>Waiting for the household.</summary>
    public const string Sent = "sent";

    /// <summary>The household picked it; the post is closed.</summary>
    public const string Picked = "picked";

    /// <summary>The worker took it back.</summary>
    public const string Withdrawn = "withdrawn";
}

/// <summary>The limits both sides keep for an offer (the columns of <c>JobOffers</c>).</summary>
public static class JobOfferRules
{
    public const int MaxNote = 300;

    /// <summary>A price fits the same range as a budget.</summary>
    public static bool IsPrice(int? pesos) => JobPostRules.IsBudget(pesos);

    /// <summary>No note, or one that fits once trimmed.</summary>
    public static bool IsNote(string? note) => note is null || note.Trim().Length <= MaxNote;
}

/// <summary>
/// <c>GET contacts/{otherUid}</c>: the other person's number (E.164), only once a picked offer links the two; null
/// before that, or when they have none. Listings never carry a number for anyone but their owner.
/// </summary>
public sealed record ContactDto(string? PhoneNumber);
