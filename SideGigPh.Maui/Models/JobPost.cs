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
    DateTimeOffset CreatedAt);

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

    /// <summary>What they are ready to pay, in pesos (their own number: Tarbaho sets no prices).</summary>
    public int? BudgetPesos { get; set; }

    public string? CityId { get; set; }

    /// <summary>Photos of the problem already uploaded, in the order added.</summary>
    public List<WorkPhoto> Photos { get; } = [];
}

/// <summary>What became of posting.</summary>
public enum JobPostResult
{
    Posted,

    /// <summary>Something on the form is missing or not valid.</summary>
    Incomplete,

    /// <summary>It didn't reach the server (no internet, server error): worth trying again.</summary>
    Failed,
}
