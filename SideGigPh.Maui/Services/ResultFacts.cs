using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services;

/// <summary>One fact on a search result's facts line ("10+ years · 8 done · ID Verified").</summary>
public enum ResultFact
{
    Experience,
    ServicesDone,
    TopBadge,
}

/// <summary>
/// What a search result row shows, kept out of the page so it is tested (Airbnb's list card: what + where with ★, the
/// name, facts joined by ·, the price): which town names the "where" and which facts make the line.
/// </summary>
public static class ResultFacts
{
    /// <summary>The Seeker's town when the worker goes there, else the first town we serve that they go to; null for none.</summary>
    public static string? TownId(ProfessionalListing listing, string? seekerTownId)
    {
        if (seekerTownId is not null && listing.CityIds.Contains(seekerTownId))
        {
            return seekerTownId;
        }

        return listing.CityIds.FirstOrDefault(id => ReferenceData.Find(ReferenceData.Cities, id) is not null);
    }

    /// <summary>Years, services done (above 0) and the first approved badge, each only when there is one.</summary>
    public static IReadOnlyList<ResultFact> Facts(ProfessionalListing listing)
    {
        var facts = new List<ResultFact>();
        if (ReferenceData.Find(ReferenceData.Experience, listing.ExperienceId) is not null)
        {
            facts.Add(ResultFact.Experience);
        }

        if (listing.CompletedJobs > 0)
        {
            facts.Add(ResultFact.ServicesDone);
        }

        if (listing.Badges.Count > 0)
        {
            facts.Add(ResultFact.TopBadge);
        }

        return facts;
    }
}
