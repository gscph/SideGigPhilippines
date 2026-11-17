using SideGigPh.Contracts;
using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services;

/// <summary>Where a completed job's blind reviews stand, for one side (<see cref="BlindReviews.StageOf"/>).</summary>
public enum BlindReviewStage
{
    /// <summary>Not marked done by both yet: no review of this job.</summary>
    NotCompleted,

    /// <summary>This person hasn't reviewed yet; what they write stays hidden until the other reviews or the window closes.</summary>
    WaitingForMe,

    /// <summary>This person reviewed; neither review shows until the other one reviews or the window closes.</summary>
    WaitingForThem,

    /// <summary>Both reviews show: the other's stars are known.</summary>
    Open,

    /// <summary>The window closed without both reviews: whatever was written shows.</summary>
    WindowClosed,
}

/// <summary>
/// Blind two-way reviews in the app (the server decides what shows, <see cref="ReviewRules.IsHidden"/>): after a
/// completed job neither side sees the other's review until both reviewed, or <see cref="ReviewRules.BlindFor"/>
/// (14 days) after the job. The picked-job card words its note from <see cref="StageOf"/>.
/// </summary>
public static class BlindReviews
{
    /// <summary>Where the reviews of <paramref name="job"/> stand at <paramref name="now"/> for this person.</summary>
    public static BlindReviewStage StageOf(PickedJob job, DateTimeOffset now) =>
        !job.IsCompleted || job.CompletedAt is not { } done ? BlindReviewStage.NotCompleted
        : job.TheirRating is not null ? BlindReviewStage.Open
        : now >= ReviewRules.OpensAt(done) ? BlindReviewStage.WindowClosed
        : job.MyRating is null ? BlindReviewStage.WaitingForMe
        : BlindReviewStage.WaitingForThem;

    /// <summary>
    /// Whole days left (1 to 14) before the reviews of <paramref name="job"/> show anyway, for "or in N days". Never more
    /// than the window: a phone clock a little behind the server's would otherwise say "in 15 days" right after the job.
    /// </summary>
    public static int DaysLeft(PickedJob job, DateTimeOffset now) =>
        job.CompletedAt is { } done
            ? Math.Clamp((int)Math.Ceiling((ReviewRules.OpensAt(done) - now).TotalDays), 1, (int)ReviewRules.BlindFor.TotalDays)
            : 0;
}
