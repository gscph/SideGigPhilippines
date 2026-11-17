namespace SideGigPh.Maui.Models;

/// <summary>
/// A job post whose offer the household picked, between this person and the one they are chatting with: the job
/// record behind verified reviews. Each side marks it done; both marks make it a completed job.
/// </summary>
/// <param name="IAmTheHousehold">This person posted it (else they are the picked worker).</param>
/// <param name="MyRating">This person's stars for the other one, or null when they haven't rated yet.</param>
public sealed record PickedJob(
    long PostId,
    string ServiceId,
    int PricePesos,
    DateTimeOffset PickedAt,
    bool IAmTheHousehold,
    bool DoneByHousehold,
    bool DoneByWorker,
    int? MyRating = null)
{
    /// <summary>Both sides marked it done.</summary>
    public bool IsCompleted => DoneByHousehold && DoneByWorker;

    /// <summary>This person already marked it done.</summary>
    public bool DoneByMe => IAmTheHousehold ? DoneByHousehold : DoneByWorker;
}
