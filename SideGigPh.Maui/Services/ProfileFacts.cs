using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services;

/// <summary>One cell of the public profile's stat row (Airbnb's "4.92 · Guest favorite · 260 Reviews").</summary>
public enum ProfileStat
{
    /// <summary>Average stars over the reviews.</summary>
    Stars,
    /// <summary>"New · no reviews yet", in place of the stars.</summary>
    NoReviews,
    /// <summary>Services done through Tarbaho (a picked offer both sides marked done).</summary>
    ServicesDone,
    /// <summary>Years of experience the worker gave.</summary>
    Experience,
}

/// <summary>
/// What a worker's public profile shows and in which order, kept out of the page so it is tested: the stat row's cells,
/// the towns (the Seeker's own first), the first name the headings use and the reviews in the sideways row.
/// </summary>
public static class ProfileFacts
{
    /// <summary>The sideways review row shows this many; "Show all" opens the rest.</summary>
    public const int ReviewsInRow = 6;

    /// <summary>Stars (or "New") always; services done and experience only when there is something to say.</summary>
    public static IReadOnlyList<ProfileStat> Stats(ProfessionalListing listing, ReviewSummary? reviews)
    {
        var stats = new List<ProfileStat> { reviews is { Count: > 0 } ? ProfileStat.Stars : ProfileStat.NoReviews };
        if (listing.CompletedJobs > 0)
        {
            stats.Add(ProfileStat.ServicesDone);
        }

        if (ReferenceData.Find(ReferenceData.Experience, listing.ExperienceId) is not null)
        {
            stats.Add(ProfileStat.Experience);
        }

        return stats;
    }

    /// <summary>The towns we serve that the worker goes to, the Seeker's own first (it is the one they care about).</summary>
    public static IReadOnlyList<string> Towns(ProfessionalListing listing, string? seekerTownId)
    {
        var towns = listing.CityIds
            .Where(id => ReferenceData.Find(ReferenceData.Cities, id) is not null)
            .Distinct()
            .ToList();
        if (seekerTownId is not null && towns.Remove(seekerTownId))
        {
            towns.Insert(0, seekerTownId);
        }

        return towns;
    }

    /// <summary>"Juan" from "Juan Dela Cruz"; an abbreviated first name keeps the next word ("Ma. Teresa").</summary>
    public static string FirstName(string displayName)
    {
        var words = displayName.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return words switch
        {
            [] => displayName.Trim(),
            [var first, var second, ..] when first.EndsWith('.') => $"{first} {second}",
            [var first, ..] => first,
        };
    }

    /// <summary>Average stars and count, or null with no reviews ("New").</summary>
    public static ReviewSummary? Summary(IReadOnlyList<Review> reviews) =>
        reviews.Count == 0 ? null : new ReviewSummary(reviews.Average(r => r.Rating), reviews.Count);

    /// <summary>The reviews in the sideways row: the ones with words first (stars alone say little), keeping their order.</summary>
    public static IReadOnlyList<Review> InRow(IReadOnlyList<Review> reviews) =>
        reviews
            .OrderBy(review => string.IsNullOrWhiteSpace(review.Comment) ? 1 : 0)
            .Take(ReviewsInRow)
            .ToList();
}
