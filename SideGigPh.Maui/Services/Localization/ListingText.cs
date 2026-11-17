using System.Globalization;
using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services.Localization;

/// <summary>
/// A listing's facts in the user's language, shared by the pro card and the pro's public
/// profile so both always say the same thing.
/// </summary>
public static class ListingText
{
    /// <summary>"Aircon Tech · Tubero"; for the "Others" trade, what the worker typed ("Others" when they typed nothing).</summary>
    public static string Trades(this AppText text, ProfessionalListing listing) =>
        string.Join(" · ", listing.CategoryIds
            .Select(id => id == ReferenceData.OtherTradeId && listing.OtherWork is { Length: > 0 } typed ? typed
                : ReferenceData.FindTrade(id) is { } trade ? text.Get(trade.Label)
                : null)
            .OfType<string>());

    /// <summary>The listing's trades still offered (or "Others"), for its trade badge (icon + label). A retired trade shows no badge.</summary>
    public static IReadOnlyList<CatalogOption> TradeOptions(this ProfessionalListing listing) =>
        listing.CategoryIds
            .Select(ReferenceData.FindTrade)
            .OfType<CatalogOption>()
            .ToList();

    /// <summary>"Taytay, Angono".</summary>
    public static string Towns(this AppText text, ProfessionalListing listing) =>
        string.Join(", ", text.Labels(ReferenceData.Cities, listing.CityIds));

    /// <summary>The jobs' labels; an "Others" job shows what the worker typed for it.</summary>
    public static IReadOnlyList<string> JobLabels(this AppText text, ProfessionalListing listing) =>
        listing.ServiceIds
            .Select(id => ReferenceData.IsOtherJob(id) && listing.OtherWork is { Length: > 0 } typed ? typed
                : ReferenceData.FindService(id) is { } option ? text.Get(option.Label)
                : null)
            .OfType<string>()
            .ToList();

    /// <summary>
    /// "₱1,000 per job" (their price for <paramref name="serviceId"/>, e.g. the job searched for), "₱755 – ₱2,500 per job"
    /// (over all their jobs), or "Price depends on the job".
    /// </summary>
    public static string Rate(this AppText text, ProfessionalListing listing, string? serviceId = null) =>
        text.RateLabel(listing, serviceId) is { } price ? text.Format(Copy.RatePerJob, price) : text.Get(Copy.RateQuote);

    /// <summary>"Exp: 3–10 years", or null when unknown.</summary>
    public static string? Experience(this AppText text, ProfessionalListing listing) =>
        ReferenceData.Find(ReferenceData.Experience, listing.ExperienceId) is { } option
            ? text.Format(Copy.ExperienceFact, text.Get(option.Label))
            : null;

    /// <summary>"10+ years" alone (the profile's stat tile), or null when unknown.</summary>
    public static string? ExperienceLabel(this AppText text, ProfessionalListing listing) =>
        ReferenceData.Find(ReferenceData.Experience, listing.ExperienceId) is { } option ? text.Get(option.Label) : null;

    /// <summary>
    /// "₱1,000" or "₱755 – ₱2,500" alone (the profile's stat tile), or null when the price depends on the job. A listing
    /// from an app version before price guides shows its old tier ("₱500 – ₱1,000").
    /// </summary>
    public static string? RateLabel(this AppText text, ProfessionalListing listing, string? serviceId = null)
    {
        if (ReferenceData.Find(ReferenceData.PriceTiers, listing.RateTierId) is { } tier)
        {
            return text.Get(tier.Label);
        }

        IEnumerable<string> jobs = serviceId is not null && listing.ServiceIds.Contains(serviceId) ? [serviceId] : listing.ServiceIds;
        return Pricing.RateRange(listing.RateTierId, jobs) is { } range ? text.Get(Pricing.PriceText(range)) : null;
    }

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
