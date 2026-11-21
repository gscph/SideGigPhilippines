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
/// <param name="MeetLatitude">The exact point the household sent the picked worker (<c>PUT picked-jobs/{postId}/location</c>),
/// only to those two and only until the job is called off or completed; null when none was sent.</param>
/// <param name="MeetSharedAt">When the household sent it.</param>
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
    bool MyReviewLocked = false,
    double? MeetLatitude = null,
    double? MeetLongitude = null,
    DateTimeOffset? MeetSharedAt = null);

/// <summary>
/// <c>PUT picked-jobs/{postId}/location</c>: the household sends the worker it picked the exact place to come to (the
/// phone's precise location, sent only on the household's tap). Sending again replaces it. Private to the two; cleared
/// when the job is called off or both mark it done.
/// </summary>
public sealed record ShareMeetLocationRequest(double Latitude, double Longitude);

/// <summary>
/// <c>POST picked-jobs/{postId}/cancel</c>: the household or the picked worker calls the job off before both marked it
/// done. The post opens again for its other offers, the numbers stop showing, the chat stays.
/// </summary>
/// <param name="Reason">One of <see cref="PickCancelReasons"/>.</param>
/// <param name="Note">Optional words, up to <see cref="PickCancelRules.MaxNote"/>.</param>
public sealed record CancelPickRequest(string Reason, string? Note = null);

/// <summary>Why a picked job was called off (the ids stored in <c>JobOffers.CancelReason</c>; the app words them).</summary>
public static class PickCancelReasons
{
    /// <summary>The worker can't do it (busy, sick, too far).</summary>
    public const string CantMakeIt = "cant-make-it";

    /// <summary>The other side stopped answering.</summary>
    public const string NoReply = "no-reply";

    /// <summary>The worker didn't come.</summary>
    public const string NoShow = "no-show";

    /// <summary>The household doesn't need it any more (fixed, found someone else).</summary>
    public const string NotNeeded = "not-needed";

    /// <summary>They couldn't agree on the price.</summary>
    public const string Price = "price";

    public const string Other = "other";

    public static readonly IReadOnlyList<string> All = [CantMakeIt, NoReply, NoShow, NotNeeded, Price, Other];

    public static bool IsKnown(string? reason) => reason is not null && All.Contains(reason);
}

/// <summary>Who called a picked job off (<c>JobOffers.CancelledBy</c>).</summary>
public static class PickCancelSides
{
    public const string Household = "household";
    public const string Worker = "worker";
}

/// <summary>The limits both sides keep for calling a job off.</summary>
public static class PickCancelRules
{
    public const int MaxNote = 300;

    /// <summary>No note, or one that fits once trimmed.</summary>
    public static bool IsNote(string? note) => note is null || note.Trim().Length <= MaxNote;
}

/// <summary><c>PUT household-reviews</c>: a worker's stars for a household, after a completed job between them. Writing again replaces it.</summary>
public sealed record SaveHouseholdReviewRequest(string SeekerUid, int Rating, string? Comment = null);
