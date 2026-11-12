using System.Globalization;
using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services.Localization;

/// <summary>
/// A listing's facts in the user's language, shared by the pro card and the pro's public
/// profile so both always say the same thing.
/// </summary>
public static class ListingText
{
    /// <summary>"Aircon Tech · Tubero".</summary>
    public static string Trades(this AppText text, ProfessionalListing listing) =>
        string.Join(" · ", text.Labels(ReferenceData.Categories, listing.CategoryIds));

    /// <summary>"Taytay, Angono".</summary>
    public static string Towns(this AppText text, ProfessionalListing listing) =>
        string.Join(", ", text.Labels(ReferenceData.Cities, listing.CityIds));

    public static IReadOnlyList<string> JobLabels(this AppText text, ProfessionalListing listing) =>
        listing.ServiceIds
            .Select(ReferenceData.FindService)
            .OfType<CatalogOption>()
            .Select(option => text.Get(option.Label))
            .ToList();

    /// <summary>"₱500 – ₱1,000 per job", or "Price depends on the job".</summary>
    public static string Rate(this AppText text, ProfessionalListing listing) =>
        ReferenceData.Find(ReferenceData.PriceTiers, listing.RateTierId) is { } tier
            ? text.Format(Copy.RatePerJob, text.Get(tier.Label))
            : text.Get(Copy.RateQuote);

    /// <summary>"Exp: 3–10 years", or null when unknown.</summary>
    public static string? Experience(this AppText text, ProfessionalListing listing) =>
        ReferenceData.Find(ReferenceData.Experience, listing.ExperienceId) is { } option
            ? text.Format(Copy.ExperienceFact, text.Get(option.Label))
            : null;

    /// <summary>"10+ years" alone (the profile's stat tile), or null when unknown.</summary>
    public static string? ExperienceLabel(this AppText text, ProfessionalListing listing) =>
        ReferenceData.Find(ReferenceData.Experience, listing.ExperienceId) is { } option ? text.Get(option.Label) : null;

    /// <summary>"₱500 – ₱1,000" alone (the profile's stat tile), or null when the price depends on the job.</summary>
    public static string? RateLabel(this AppText text, ProfessionalListing listing) =>
        ReferenceData.Find(ReferenceData.PriceTiers, listing.RateTierId) is { } tier ? text.Get(tier.Label) : null;

    /// <summary>10+ years on the job: shown in the brand color with an award icon.</summary>
    public static bool IsSeasoned(this ProfessionalListing listing) => listing.ExperienceId == ReferenceData.TopExperienceId;

    /// <summary>"4.7 ★ (3)".</summary>
    public static string Stars(this AppText text, ReviewSummary reviews) =>
        text.Format(Copy.StatRating, reviews.Average.ToString("0.0", CultureInfo.InvariantCulture), reviews.Count);

    private static IEnumerable<string> Labels(this AppText text, IEnumerable<CatalogOption> options, IEnumerable<string> ids) =>
        ids.Select(id => ReferenceData.Find(options, id))
            .OfType<CatalogOption>()
            .Select(option => text.Get(option.Label));
}
