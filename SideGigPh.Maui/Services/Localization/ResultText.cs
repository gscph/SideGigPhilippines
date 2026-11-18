using System.Globalization;
using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services.Localization;

/// <summary>A search result row's words and the results' group headings, in the user's language (tested).</summary>
public static class ResultText
{
    /// <summary>Line 1: "Aircon Technician in Angono" (<see cref="ResultFacts.TownId"/>); the trades alone without a town.</summary>
    public static string WhatAndWhere(this AppText text, ProfessionalListing listing, string? seekerTownId)
    {
        var trades = text.Trades(listing);
        var town = ReferenceData.Find(ReferenceData.Cities, ResultFacts.TownId(listing, seekerTownId)) is { } city ? text.Get(city.Label) : null;
        return town is null ? trades
            : trades.Length == 0 ? town
            : text.Format(Copy.ResultWhatWhere, trades, town);
    }

    /// <summary>Line 3: "10+ years · 8 done · ID Verified", or null when there is nothing to say.</summary>
    public static string? FactsLine(this AppText text, ProfessionalListing listing)
    {
        var parts = ResultFacts.Facts(listing).Select(fact => fact switch
        {
            ResultFact.Experience => text.ExperienceLabel(listing),
            ResultFact.ServicesDone => text.Format(Copy.ResultDone, listing.CompletedJobs),
            ResultFact.TopBadge => text.Badge(listing.Badges[0]),
            _ => null,
        }).OfType<string>().ToList();
        return parts.Count == 0 ? null : string.Join(" · ", parts);
    }

    /// <summary>"★ 4.9 (12)" without the star (the row draws it in gold), or "New".</summary>
    public static string StarsShort(this AppText text, ReviewSummary? reviews) =>
        reviews is { Count: > 0 }
            ? $"{reviews.Average.ToString("0.0", CultureInfo.InvariantCulture)} ({reviews.Count})"
            : text.Get(Copy.StatNew);

    /// <summary>"9 skilled workers in Angono".</summary>
    public static string InTownHeading(this AppText text, int total, string town) =>
        total == 1 ? text.Format(Copy.ResultsInTownOne, town) : text.Format(Copy.ResultsInTownCount, total, town);

    /// <summary>"3 more in towns nearby", or "None in Angono yet · 3 in towns nearby" when the town has nobody.</summary>
    public static string NearbyHeading(this AppText text, int nearbyTotal, int inTownTotal, string town) =>
        inTownTotal > 0 ? text.Format(Copy.ResultsMoreNearby, nearbyTotal) : text.Format(Copy.ResultsOnlyNearby, town, nearbyTotal);
}
