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

/// <summary>
/// How a worker behaves on the app, read by the server for ranking and never sent to phones.
/// <paramref name="ChatsReceived"/> / <paramref name="ChatsAnswered"/>: chats Seekers started with them in the last
/// <see cref="SearchRanker.ResponsivenessWindow"/> that they answered in the chat (a chat the Seeker only called, or one
/// younger than <see cref="SearchRanker.ReplyGrace"/> with no reply yet, isn't counted).
/// <paramref name="AverageReplyMinutes"/>: how long their first reply took, each capped at a day; null with no reply.
/// <paramref name="LastActiveAt"/>: when they last opened their Inbox (or last edited the listing).
/// <paramref name="JoinedAt"/>: when their account was made.
/// </summary>
public sealed record WorkerSignals(
    int ChatsReceived,
    int ChatsAnswered,
    double? AverageReplyMinutes,
    DateTimeOffset LastActiveAt,
    DateTimeOffset JoinedAt);

/// <summary>A listing with its <see cref="WorkerSignals"/>: what the ranker orders.</summary>
public sealed record RankCandidate(ListingDto Listing, WorkerSignals Signals);

/// <summary>
/// One worker's score, out of 100, and its parts (each already weighted), so a ranking can be explained:
/// "ranked high on reviews and replies, low on price".
/// </summary>
public sealed record RankScore(double Reviews, double Responsiveness, double Activity, double BudgetFit, double Experience)
{
    public double Total => Reviews + Responsiveness + Activity + BudgetFit + Experience;
}

/// <summary>Ranked results: workers who go to the Seeker's town, then workers from other served towns.</summary>
public sealed record RankedResult(IReadOnlyList<SearchHitDto> InTown, IReadOnlyList<SearchHitDto> OtherTowns)
{
    /// <summary>
    /// One page of the ranked list, in-town first, with both groups' totals so the app can title them.
    /// <see cref="SearchPageDto.ServiceFallback"/> is always false now (the job is a hard filter); it stays in the
    /// contract for app versions that read it.
    /// </summary>
    public SearchPageDto Page(int skip, int take) => new(
        [.. InTown.Concat(OtherTowns).Skip(skip).Take(take)],
        InTown.Count,
        OtherTowns.Count,
        ServiceFallback: false);
}

/// <summary>
/// Orders a trade's workers for one Seeker, on the server so results come a page at a time. Same shape as big
/// marketplaces' search: hard filters first (retrieval), then one score per worker (ranking). Paid boosts, when they
/// come, are labeled slots placed after ranking, never part of this score (docs/search-ranking.md).
/// <list type="number">
/// <item>Job (filter): only workers who list the exact job. Nobody listing it = no results.</item>
/// <item>Town (filter, forgiving): workers who go to the Seeker's town come first, in their own group; workers from
/// other served towns follow as "nearby"; a worker only in towns we don't serve is left out.</item>
/// <item>Score (0–100, within a group): reviews 30, responsiveness 20, activity and tenure 20, budget fit 20,
/// experience 10. Every part is a 0–1 feature times a weight below. A new worker with no reviews or chats gets
/// neutral priors, so they land mid-list instead of last and can earn their first customers.</item>
/// </list>
/// </summary>
public static class SearchRanker
{
    // Weights: the points each part is worth. They add up to 100.
    public const double ReviewStarsWeight = 22;
    public const double ReviewCountWeight = 8;
    public const double ReplyRateWeight = 12;
    public const double ReplySpeedWeight = 8;
    public const double RecentActivityWeight = 14;
    public const double TenureWeight = 6;
    public const double BudgetFitWeight = 20;
    public const double ExperienceWeight = 10;

    // Reviews: stars are smoothed toward a prior (a Bayesian average) so 1 lucky 5-star can't beat 40 good reviews,
    // and the count itself earns points, on a log scale (the 5th review matters more than the 45th).
    private const double PriorStars = 4.0;              // what an unreviewed worker is assumed to be
    private const double PriorReviews = 3;              // how many reviews the prior counts as
    private const double ReviewsForFullCount = 25;

    // Responsiveness: the share of chats answered (smoothed like stars) and how fast the first reply came.
    public static readonly TimeSpan ResponsivenessWindow = TimeSpan.FromDays(90);
    public static readonly TimeSpan ReplyGrace = TimeSpan.FromHours(24);
    private const double PriorReplyRate = 0.7;          // a new worker is assumed to answer most chats
    private const double PriorChats = 3;
    private const double ReplyMinutesForHalf = 60;      // a first reply after 1 hour earns half the speed points
    private const double PriorReplyMinutes = 60;

    // Activity: points halve for every week since the worker last opened the app. Tenure: full after 6 months.
    private const double ActivityHalfLifeDays = 7;
    private const double TenureDaysForFull = 180;

    // Budget: within budget earns it all, "depends on the job" (no price) some, each tier over budget loses half.
    private const double QuoteFit = 0.4;
    private const double FitLostPerTierOver = 0.5;

    public static RankedResult Rank(IReadOnlyList<RankCandidate> pros, RankRequest request, RankScales scales, DateTimeOffset now)
    {
        var service = request.ServiceId;
        var hits = pros
            .Where(p => service is null || p.Listing.ServiceIds.Contains(service))
            .Select(pro => (Hit: Hit(pro.Listing, request, scales), Score: Score(pro, request, scales, now).Total))
            .OrderByDescending(scored => scored.Score)
            .ThenBy(scored => scored.Hit.Listing.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(scored => scored.Hit.Listing.Id, StringComparer.Ordinal)
            .Select(scored => scored.Hit)
            .ToList();

        if (request.CityId is null)
        {
            return new RankedResult(hits, []);
        }

        // "Nearby" = another served town (e.g. Antipolo for a Taytay Seeker), and only for a town we serve.
        var served = scales.TownIds.Contains(request.CityId);
        return new RankedResult(
            [.. hits.Where(h => h.ServesTown)],
            [.. hits.Where(h => !h.ServesTown && served && h.Listing.CityIds.Any(scales.TownIds.Contains))]);
    }

    /// <summary>The score that orders workers inside a group (higher first), part by part.</summary>
    public static RankScore Score(RankCandidate pro, RankRequest request, RankScales scales, DateTimeOffset now)
    {
        var listing = pro.Listing;
        var signals = pro.Signals;
        return new RankScore(
            Reviews: (ReviewStarsWeight * StarsFeature(listing.Reviews)) + (ReviewCountWeight * ReviewCountFeature(listing.Reviews)),
            Responsiveness: (ReplyRateWeight * ReplyRateFeature(signals)) + (ReplySpeedWeight * ReplySpeedFeature(signals)),
            Activity: (RecentActivityWeight * RecentFeature(signals, now)) + (TenureWeight * TenureFeature(signals, now)),
            BudgetFit: BudgetFitWeight * BudgetFeature(listing, request, scales),
            Experience: ExperienceWeight * ExperienceFeature(listing, scales));
    }

    /// <summary>Smoothed stars on 1–5, as 0–1.</summary>
    private static double StarsFeature(ReviewSummaryDto? reviews)
    {
        var stars = reviews is null
            ? PriorStars
            : ((PriorStars * PriorReviews) + (reviews.Average * reviews.Count)) / (PriorReviews + reviews.Count);
        return Clamp((stars - 1) / 4);
    }

    private static double ReviewCountFeature(ReviewSummaryDto? reviews) =>
        reviews is null ? 0 : Clamp(Math.Log(1 + reviews.Count) / Math.Log(1 + ReviewsForFullCount));

    private static double ReplyRateFeature(WorkerSignals s) =>
        ((PriorReplyRate * PriorChats) + s.ChatsAnswered) / (PriorChats + s.ChatsReceived);

    /// <summary>
    /// Speed of each answered chat, an unanswered chat as 0 (never replying must not beat replying late), smoothed
    /// toward the speed of <see cref="PriorReplyMinutes"/> like the reply rate.
    /// </summary>
    private static double ReplySpeedFeature(WorkerSignals s)
    {
        var answeredSpeed = s.AverageReplyMinutes is { } minutes ? Speed(minutes) * s.ChatsAnswered : 0;
        return ((Speed(PriorReplyMinutes) * PriorChats) + answeredSpeed) / (PriorChats + s.ChatsReceived);

        static double Speed(double minutes) => 1 / (1 + (Math.Max(0, minutes) / ReplyMinutesForHalf));
    }

    private static double RecentFeature(WorkerSignals s, DateTimeOffset now) =>
        Math.Pow(0.5, Math.Max(0, (now - s.LastActiveAt).TotalDays) / ActivityHalfLifeDays);

    private static double TenureFeature(WorkerSignals s, DateTimeOffset now) =>
        Clamp((now - s.JoinedAt).TotalDays / TenureDaysForFull);

    /// <summary>No budget given: every price fits.</summary>
    private static double BudgetFeature(ListingDto pro, RankRequest request, RankScales scales)
    {
        var budget = Position(scales.PriceTierIds, request.BudgetId);
        var rate = Position(scales.PriceTierIds, pro.RateTierId);
        return budget < 0 ? 1
            : rate < 0 ? QuoteFit
            : rate <= budget ? 1
            : Clamp(1 - ((rate - budget) * FitLostPerTierOver));
    }

    /// <summary>Unknown experience counts as the least.</summary>
    private static double ExperienceFeature(ListingDto pro, RankScales scales) =>
        scales.ExperienceIds.Count < 2 ? 0 : Math.Max(0, Position(scales.ExperienceIds, pro.ExperienceId)) / (double)(scales.ExperienceIds.Count - 1);

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

    private static double Clamp(double value) => Math.Clamp(value, 0, 1);

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
