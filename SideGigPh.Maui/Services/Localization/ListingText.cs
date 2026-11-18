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
        text.Jobs(listing).Select(job => job.Label).ToList();

    /// <summary>Each job's id (for its picture) and label; an "Others" job shows what the worker typed, an unknown id is skipped.</summary>
    public static IReadOnlyList<(string Id, string Label)> Jobs(this AppText text, ProfessionalListing listing)
    {
        var jobs = new List<(string Id, string Label)>();
        foreach (var id in listing.ServiceIds)
        {
            if (ReferenceData.IsOtherJob(id) && listing.OtherWork is { Length: > 0 } typed)
            {
                jobs.Add((id, typed));
            }
            else if (ReferenceData.FindService(id) is { } option)
            {
                jobs.Add((id, text.Get(option.Label)));
            }
        }

        return jobs;
    }

    /// <summary>The profile's line under the name: "Aircon Technician in Angono" (the first of <paramref name="townIds"/>).</summary>
    public static string TradeIn(this AppText text, ProfessionalListing listing, IReadOnlyList<string> townIds)
    {
        var trades = text.Trades(listing);
        var town = townIds.Count > 0 && ReferenceData.Find(ReferenceData.Cities, townIds[0]) is { } city ? text.Get(city.Label) : null;
        return (trades.Length > 0, town) switch
        {
            (true, { } where) => text.Format(Copy.ProfileTradeIn, trades, where),
            (false, { } where) => where,
            _ => trades,
        };
    }

    /// <summary>"Also goes to Antipolo · Taytay" (the rest of <paramref name="townIds"/>), or null when there is one town.</summary>
    public static string? AlsoGoesTo(this AppText text, IReadOnlyList<string> townIds) =>
        townIds.Count < 2 ? null
            : text.Format(Copy.ProfileAlsoGoesTo, string.Join(" · ", text.Labels(ReferenceData.Cities, townIds.Skip(1))));

    /// <summary>"₱1,000 average per service", or "No price yet" on a draft before the price question.</summary>
    public static string Rate(this AppText text, ProfessionalListing listing) =>
        listing.RateLabel() is { } price ? text.Format(Copy.RatePerJob, price) : text.Get(Copy.RateNotSet);

    /// <summary>"Exp: 3–10 years", or null when unknown.</summary>
    public static string? Experience(this AppText text, ProfessionalListing listing) =>
        ReferenceData.Find(ReferenceData.Experience, listing.ExperienceId) is { } option
            ? text.Format(Copy.ExperienceFact, text.Get(option.Label))
            : null;

    /// <summary>"10+ years" alone (the profile's stat tile), or null when unknown.</summary>
    public static string? ExperienceLabel(this AppText text, ProfessionalListing listing) =>
        ReferenceData.Find(ReferenceData.Experience, listing.ExperienceId) is { } option ? text.Get(option.Label) : null;

    /// <summary>"₱1,000" alone (their average price: results, the profile's bar), or null on a draft before the price question.</summary>
    public static string? RateLabel(this ProfessionalListing listing) => listing.RatePesos is { } pesos ? Money.Peso(pesos) : null;

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
