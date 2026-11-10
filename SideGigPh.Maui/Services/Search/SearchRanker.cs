using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services.Search;

/// <summary>
/// Orders a category's pros for one Seeker. Filters first, then scores:
/// <list type="number">
/// <item>Job: only pros who list the exact job, unless nobody does yet (then the whole category, flagged).</item>
/// <item>Town: pros who go to the Seeker's town come first, in their own group; other towns follow.</item>
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
    private const double PriorStars = 4.0;        // what an unreviewed pro is assumed to be
    private const double PriorWeight = 3;         // how many reviews the prior counts as
    private const double ExperienceWeight = 3;    // per experience step (0–3)
    private const double PhotoBonus = 3;
    private const double WorkPhotosBonus = 4;

    public static SearchResult Rank(
        IReadOnlyList<ProfessionalListing> pros,
        SearchRequest request,
        IReadOnlyDictionary<string, ReviewSummary> reviews)
    {
        var service = request.ServiceId;
        var offering = service is null ? pros : pros.Where(p => p.ServiceIds.Contains(service)).ToList();
        var fallback = service is not null && offering.Count == 0 && pros.Count > 0;

        var hits = (fallback ? pros : offering)
            .Select(pro => Score(pro, request, reviews.GetValueOrDefault(pro.Id)))
            .OrderByDescending(hit => hit.Score)
            .ThenBy(hit => hit.Pro.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return request.CityId is null
            ? new SearchResult(hits, [], fallback)
            : new SearchResult(hits.Where(h => h.ServesTown).ToList(), hits.Where(h => !h.ServesTown).ToList(), fallback);
    }

    private static SearchHit Score(ProfessionalListing pro, SearchRequest request, ReviewSummary? reviews)
    {
        var budget = ReferenceData.PriceRank(request.BudgetId);
        var rate = ReferenceData.PriceRank(pro.RateTierId);
        var fitsBudget = budget >= 0 && rate >= 0 && rate <= budget;

        double score = 0;
        if (fitsBudget)
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

        var stars = reviews is null
            ? PriorStars
            : ((PriorStars * PriorWeight) + (reviews.Average * reviews.Count)) / (PriorWeight + reviews.Count);
        score += (stars - 3) * StarsWeight;
        score += ExperienceRank(pro.ExperienceId) * ExperienceWeight;
        score += pro.PhotoUrl is null ? 0 : PhotoBonus;
        score += pro.WorkPhotoUrls.Count == 0 ? 0 : WorkPhotosBonus;

        return new SearchHit(
            pro,
            reviews,
            OffersService: request.ServiceId is not null && pro.ServiceIds.Contains(request.ServiceId),
            ServesTown: request.CityId is not null && pro.CityIds.Contains(request.CityId),
            FitsBudget: fitsBudget,
            Score: score);
    }

    private static int ExperienceRank(string? experienceId)
    {
        for (var i = 0; i < ReferenceData.Experience.Count; i++)
        {
            if (ReferenceData.Experience[i].Id == experienceId)
            {
                return i;
            }
        }

        return 0;
    }
}
