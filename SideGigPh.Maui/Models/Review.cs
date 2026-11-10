namespace SideGigPh.Maui.Models;

/// <summary>
/// A Seeker's rating of a Professional they contacted. <see cref="Rating"/> is 1–5 stars.
/// One review per Seeker per pro; writing again replaces it.
/// </summary>
public sealed record Review(
    string Id,
    string ProfessionalId,
    string SeekerId,
    string ReviewerName,
    int Rating,
    string? Comment,
    string? ServiceId,
    DateTimeOffset At);

/// <summary>A pro's average stars and how many reviews it's based on.</summary>
public sealed record ReviewSummary(double Average, int Count);
