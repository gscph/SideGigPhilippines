namespace SideGigPh.Maui.Models;

/// <summary>
/// A job post whose offer the household picked, between this person and the one they are chatting with: the job
/// record behind verified reviews. Each side marks it done; both marks make it a completed job.
/// </summary>
/// <param name="IAmTheHousehold">This person posted it (else they are the picked worker).</param>
/// <param name="MyRating">This person's stars for the other one, or null when they haven't rated yet.</param>
/// <param name="CompletedAt">When the second side marked it done (null until both did).</param>
/// <param name="TheyReviewed">The other one reviewed this person since the job was completed (hidden until this person reviews too).</param>
/// <param name="TheirRating">The other one's stars for this person, only once they show (blind reviews, <c>Services/BlindReviews</c>).</param>
/// <param name="MyReviewLocked">This person's review of the other can't be changed any more: both reviews of the job show.</param>
/// <param name="MeetPoint">The exact place the household sent the picked worker (<c>Services/MeetLocation</c>), until the job is called off or done.</param>
/// <param name="MeetSharedAt">When the household sent it.</param>
public sealed record PickedJob(
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
    GeoPoint? MeetPoint = null,
    DateTimeOffset? MeetSharedAt = null)
{
    /// <summary>Both sides marked it done.</summary>
    public bool IsCompleted => DoneByHousehold && DoneByWorker;

    /// <summary>This person already marked it done.</summary>
    public bool DoneByMe => IAmTheHousehold ? DoneByHousehold : DoneByWorker;
}
