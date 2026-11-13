using SideGigPh.Contracts;

namespace SideGigPh.AzureSQL;

/// <summary>What a Seeker searched for, as ids: the job (null = the whole trade), their town and their budget.</summary>
public sealed record RankRequest(string? ServiceId, string? CityId, string? BudgetId);

/// <summary>
/// The config lists ranking compares by position: price tiers cheapest first, experience least first, and the towns we
/// serve (all in one area, so "nearby" = any other served town).
/// </summary>
public sealed record RankScales(IReadOnlyList<string> PriceTierIds, IReadOnlyList<string> ExperienceIds, IReadOnlySet<string> TownIds)
{
    public static RankScales From(ConfigDto config) => new(
        [.. config.PriceTiers.Select(t => t.Id)],
        [.. config.Experience.Select(e => e.Id)],
        config.Towns.Select(t => t.Id).ToHashSet());
}

/// <summary>Ranked results: workers who go to the Seeker's town, then workers from other served towns.</summary>
public sealed record RankedResult(IReadOnlyList<SearchHitDto> InTown, IReadOnlyList<SearchHitDto> OtherTowns, bool ServiceFallback)
{
    /// <summary>One page of the ranked list, in-town first, with both groups' totals so the app can title them.</summary>
    public SearchPageDto Page(int skip, int take) => new(
        [.. InTown.Concat(OtherTowns).Skip(skip).Take(take)],
        InTown.Count,
        OtherTowns.Count,
        ServiceFallback);
}

/// <summary>
/// Orders a trade's workers for one Seeker, on the server so results can come a page at a time. Filters first, then scores:
/// <list type="number">
/// <item>Job: only workers who list the exact job, unless nobody does yet (then the whole trade, flagged).</item>
/// <item>Town: workers who go to the Seeker's town come first, in their own group; workers from other served towns
/// follow; a worker who only goes to towns we don't serve is left out.</item>
/// <item>Score within a group: fits the budget, stars (smoothed so 1 review can't beat 20),
/// experience, and photos (customers trust a face and real work).</item>
/// </list>
/// Every weight is a constant here so the ranking stays explainable.
/// </summary>
public static class SearchRanker
{
    private const double FitsBudgetBonus = 30;
    private const double QuoteBonus = 8;          // "Depends on the job": no price to compare
    private const double OverBudgetPenaltyPerTier = 10;
    private const double StarsWeight = 8;         // per star above 3, after smoothing
    private const double PriorStars = 4.0;        // what an unreviewed worker is assumed to be
    private const double PriorWeight = 3;         // how many reviews the prior counts as
    private const double ExperienceWeight = 3;    // per experience step (0–3)
    private const double PhotoBonus = 3;
    private const double WorkPhotosBonus = 4;

    public static RankedResult Rank(IReadOnlyList<ListingDto> pros, RankRequest request, RankScales scales)
    {
        var service = request.ServiceId;
        var offering = service is null ? pros : pros.Where(p => p.ServiceIds.Contains(service)).ToList();
        var fallback = service is not null && offering.Count == 0 && pros.Count > 0;

        var hits = (fallback ? pros : offering)
            .Select(pro => (Hit: Hit(pro, request, scales), Score: Score(pro, request, scales)))
            .OrderByDescending(scored => scored.Score)
            .ThenBy(scored => scored.Hit.Listing.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(scored => scored.Hit.Listing.Id, StringComparer.Ordinal)
            .Select(scored => scored.Hit)
            .ToList();

        if (request.CityId is null)
        {
            return new RankedResult(hits, [], fallback);
        }

        // "Nearby" = another served town (e.g. Antipolo for a Taytay Seeker), and only for a town we serve.
        var served = scales.TownIds.Contains(request.CityId);
        return new RankedResult(
            [.. hits.Where(h => h.ServesTown)],
            [.. hits.Where(h => !h.ServesTown && served && h.Listing.CityIds.Any(scales.TownIds.Contains))],
            fallback);
    }

    /// <summary>The score that orders workers inside a group (higher first).</summary>
    public static double Score(ListingDto pro, RankRequest request, RankScales scales)
    {
        var budget = Position(scales.PriceTierIds, request.BudgetId);
        var rate = Position(scales.PriceTierIds, pro.RateTierId);

        double score = 0;
        if (budget >= 0 && rate >= 0 && rate <= budget)
        {
            score += FitsBudgetBonus;
        }
        else if (budget >= 0 && rate < 0)
        {
            score += QuoteBonus;
        }
        else if (budget >= 0)
        {
            score -= (rate - budget) * OverBudgetPenaltyPerTier;
        }

        var reviews = pro.Reviews;
        var stars = reviews is null
            ? PriorStars
            : ((PriorStars * PriorWeight) + (reviews.Average * reviews.Count)) / (PriorWeight + reviews.Count);
        score += (stars - 3) * StarsWeight;
        score += Math.Max(0, Position(scales.ExperienceIds, pro.ExperienceId)) * ExperienceWeight;
        score += pro.ProfilePhoto is null ? 0 : PhotoBonus;
        score += pro.WorkPhotos.Count == 0 ? 0 : WorkPhotosBonus;
        return score;
    }

    private static SearchHitDto Hit(ListingDto pro, RankRequest request, RankScales scales)
    {
        var budget = Position(scales.PriceTierIds, request.BudgetId);
        var rate = Position(scales.PriceTierIds, pro.RateTierId);
        return new SearchHitDto(
            pro,
            OffersService: request.ServiceId is not null && pro.ServiceIds.Contains(request.ServiceId),
            ServesTown: request.CityId is not null && pro.CityIds.Contains(request.CityId),
            FitsBudget: budget >= 0 && rate >= 0 && rate <= budget);
    }

    /// <summary>Position on a config list, or -1 for an id that isn't on it ("not sure", "quote", unanswered).</summary>
    private static int Position(IReadOnlyList<string> ids, string? id)
    {
        for (var i = 0; i < ids.Count; i++)
        {
            if (ids[i] == id)
            {
                return i;
            }
        }

        return -1;
    }
}
