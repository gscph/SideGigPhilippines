namespace SideGigPh.Maui.Models;

/// <summary>
/// A Seeker's rating of a Professional they contacted. <see cref="Rating"/> is 1–5 stars.
/// One review per Seeker per pro; writing again replaces it. <see cref="VerifiedJob"/>: the Seeker picked this pro's
/// offer on a post and both marked the work done. <see cref="Hidden"/>: a blind review only its writer sees for now (it
/// shows once the worker reviews back, or 14 days after the job). <see cref="Locked"/>: this person's own review is about a
/// completed job and both reviews show, so it can't be changed any more.
/// </summary>
public sealed record Review(
    string Id,
    string ProfessionalId,
    string SeekerId,
    string ReviewerName,
    int Rating,
    string? Comment,
    string? ServiceId,
    DateTimeOffset At,
    bool VerifiedJob = false,
    bool Hidden = false,
    bool Locked = false);

/// <summary>A pro's average stars and how many reviews it's based on.</summary>
public sealed record ReviewSummary(double Average, int Count);
