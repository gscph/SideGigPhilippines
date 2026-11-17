namespace SideGigPh.Contracts;

/// <summary>
/// A job post whose offer the household picked, between the caller and one other person (<c>GET picked-jobs/{otherUid}</c>):
/// the job record behind verified reviews. Each side marks it done (<c>POST picked-jobs/{postId}/done</c>); once both
/// did, it is a completed job: the household's review of the worker shows "Verified job", the worker can rate the
/// household, and it counts on the worker's profile.
/// </summary>
/// <param name="IAmTheHousehold">The caller posted it (else the caller is the picked worker).</param>
/// <param name="MyRating">The caller's stars for the other person (household → worker in <c>Reviews</c>, worker → household in <c>HouseholdReviews</c>), or null.</param>
/// <param name="CompletedAt">When the second side marked it done (null until both did). Blind reviews open
/// <see cref="ReviewRules.BlindFor"/> after it at the latest.</param>
/// <param name="TheyReviewed">The other person has reviewed the caller since this job was completed (their stars stay hidden until the caller reviews too).</param>
/// <param name="TheirRating">The other person's stars for the caller, only once they show (<see cref="ReviewRules.IsHidden"/>), else null.</param>
/// <param name="MyReviewLocked">The caller's review of the other can't be changed any more (<see cref="ReviewRules.IsLocked"/>).</param>
public sealed record PickedJobDto(
    long PostId,
    string ServiceId,
    int PricePesos,
    DateTimeOffset PickedAt,
    bool IAmTheHousehold,
    bool DoneByHousehold,
    bool DoneByWorker,
    int? MyRating = null,
    DateTimeOffset? CompletedAt = null,
    bool TheyReviewed = false,
    int? TheirRating = null,
    bool MyReviewLocked = false);

/// <summary><c>PUT household-reviews</c>: a worker's stars for a household, after a completed job between them. Writing again replaces it.</summary>
public sealed record SaveHouseholdReviewRequest(string SeekerUid, int Rating, string? Comment = null);
