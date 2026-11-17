namespace SideGigPh.Contracts;

/// <summary>
/// A job post whose offer the household picked, between the caller and one other person (<c>GET picked-jobs/{otherUid}</c>):
/// the job record behind verified reviews. Each side marks it done (<c>POST picked-jobs/{postId}/done</c>); once both
/// did, it is a completed job: the household's review of the worker shows "Verified job", the worker can rate the
/// household, and it counts on the worker's profile.
/// </summary>
/// <param name="IAmTheHousehold">The caller posted it (else the caller is the picked worker).</param>
/// <param name="MyRating">The caller's stars for the other person (household → worker in <c>Reviews</c>, worker → household in <c>HouseholdReviews</c>), or null.</param>
public sealed record PickedJobDto(
    long PostId,
    string ServiceId,
    int PricePesos,
    DateTimeOffset PickedAt,
    bool IAmTheHousehold,
    bool DoneByHousehold,
    bool DoneByWorker,
    int? MyRating = null);

/// <summary><c>PUT household-reviews</c>: a worker's stars for a household, after a completed job between them. Writing again replaces it.</summary>
public sealed record SaveHouseholdReviewRequest(string SeekerUid, int Rating, string? Comment = null);
