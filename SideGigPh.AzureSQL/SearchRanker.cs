using SideGigPh.Contracts;

namespace SideGigPh.AzureSQL;

/// <summary>
/// What a Seeker searched for, as ids: the job (null = the whole trade), their town, their budget (a
/// <see cref="BudgetRange"/> id such as <c>800-1500</c>), for an "Others" job (<see cref="OtherWork"/>) what they typed
/// (workers whose own "Others" text shares words with it come first), and how to order the results
/// (<see cref="SearchSort"/>).
/// </summary>
public sealed record RankRequest(
    string? ServiceId, string? CityId, string? BudgetId, string? OtherText = null, string Sort = SearchSort.BestMatch);

/// <summary>
/// What ranking reads from the config: each job's price guide (what a price is compared with when the Seeker's budget
/// has no top), experience least first, and the towns we serve (all in one area, so "nearby" = any other served town).
/// </summary>
public sealed record RankScales(
    IReadOnlyDictionary<string, PriceGuideDto> Prices,
    IReadOnlyList<string> ExperienceIds,
    IReadOnlySet<string> TownIds)
{
    public static RankScales From(ConfigDto config) => new(
        config.Catalog.SelectMany(c => c.Services).Where(s => s.Price is not null).ToDictionary(s => s.Id, s => s.Price!),
        [.. config.Experience.Select(e => e.Id)],
        config.Towns.Select(t => t.Id).ToHashSet());

    /// <summary>
    /// What a worker's price is measured against: the top of the Seeker's budget (<c>OverLoses</c>: a price over it
    /// loses fit). Without a top (no budget, or "₱1,500 and up"): the high price of the job's guide (the whole trade:
    /// the highest of the worker's jobs' guides), at least the budget's low end, and being over it only stops the
    /// cheaper-is-better bonus. Null when nothing is known.
    /// </summary>
    public (int Pesos, bool OverLoses)? PriceReference(ListingDto pro, RankRequest request)
    {
        var budget = BudgetRange.TryParse(request.BudgetId, out var low, out var top);
        if (budget && top is { } max)
        {
            return (max, true);
        }

        IEnumerable<string> jobs = request.ServiceId is not null ? [request.ServiceId] : pro.ServiceIds;
        var highs = jobs.Select(id => Prices.TryGetValue(id, out var guide) ? guide.High : (int?)null).OfType<int>().ToList();
        int? reference = highs.Count == 0 ? (budget ? low : null) : Math.Max(highs.Max(), budget ? low : 0);
        return reference is { } pesos ? (pesos, false) : null;
    }
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
/// <param name="Trust">Points for badges an operator approved (<see cref="SearchRanker.TrustFeature"/>), on top of the 100.</param>
/// <param name="Photos">Points for work photos on the listing (<see cref="SearchRanker.PhotosFeature"/>), on top of the 100.</param>
public sealed record RankScore(double Reviews, double Responsiveness, double Activity, double PriceFit, double Experience, double Trust = 0, double Photos = 0)
{
    public double Total => Reviews + Responsiveness + Activity + PriceFit + Experience + Trust + Photos;
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
/// <item>Score (0–100, within a group): reviews 30, responsiveness 20, activity and tenure 20, price fit 20 (cheaper is higher; over the Seeker's budget is lower, never hidden),
/// experience 10. Every part is a 0–1 feature times a weight below. A new worker with no reviews or chats gets
/// neutral priors, so they land mid-list instead of last and can earn their first customers.</item>
/// <item>Badges (up to +13 on top): ID Verified 4, NBI / police clearance 3, TESDA Certified 3, a past project with a developer or company 3.
/// A bonus, not a share of the 100, so a worker without badges scores exactly as before and one with them moves up:
/// proof a person checked is the one signal a new worker can earn on day one.</item>
/// <item>Work photos (up to +3 on top): 3 for <see cref="FullPhotoBonusAt"/> or more, half for one. Optional (a
/// listing publishes without them), so also a bonus: a worker without photos scores exactly as before. Worth as much
/// as a TESDA or project badge, less than an ID, because nobody checks them; capped at 2 so a worker can't climb by
/// adding more photos, only by showing some.</item>
/// <item>Sort: <see cref="SearchSort.LowestPrice"/> orders each group cheapest first, the score breaking ties.</item>
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
    public const double PriceFitWeight = 20;
    public const double ExperienceWeight = 10;

    // Badges: bonus points on top of the 100, one amount per kind of badge (several project badges count once).
    public const double IdBadgeWeight = 4;
    public const double TesdaBadgeWeight = 3;
    public const double ProjectBadgeWeight = 3;
    public const double ClearanceBadgeWeight = 3;

    // Work photos: bonus points on top of the 100. Full at FullPhotoBonusAt photos, half for a single one.
    public const double WorkPhotosWeight = 3;
    public const int FullPhotoBonusAt = 2;

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

    // Price: measured against the top of the Seeker's budget (else the job's guide, RankScales.PriceReference). Within
    // it, cheaper earns more: from all of it at ₱0 down to (1 - CheapnessShare) at the top. Over a budget's top, the rest
    // is lost in step with how far over: 50% over keeps half, double keeps none. Never a filter: over budget still shows.
    private const double CheapnessShare = 0.3;

    public static RankedResult Rank(IReadOnlyList<RankCandidate> pros, RankRequest request, RankScales scales, DateTimeOffset now)
    {
        var service = request.ServiceId;
        var typed = OtherWork.IsOther(service) ? FeedbackMatch.Words(request.OtherText) : new HashSet<string>();
        var hits = pros
            .Where(p => service is null || p.Listing.ServiceIds.Contains(service))
            .Select(pro => (Hit: Hit(pro.Listing, request), Score: Score(pro, request, scales, now).Total,
                Words: OtherWordsMatch(typed, pro.Listing)))
            .OrderBy(scored => request.Sort == SearchSort.LowestPrice ? scored.Hit.Listing.RatePesos : 0)
            .ThenByDescending(scored => scored.Words)
            .ThenByDescending(scored => scored.Score)
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
            PriceFit: PriceFitWeight * PriceFeature(listing, request, scales),
            Experience: ExperienceWeight * ExperienceFeature(listing, scales),
            Trust: TrustFeature(listing),
            Photos: PhotosFeature(listing));
    }

    /// <summary>
    /// For an "Others" search: the share of the words the Seeker typed that the worker's own "Others" text also has
    /// (<see cref="FeedbackMatch"/>: small words dropped, plurals ignored), 0 to 1. It orders before the score, so
    /// "plaster sa kisame" finds the worker who wrote "kisame at plaster" first; 0 for any other search.
    /// </summary>
    public static double OtherWordsMatch(IReadOnlySet<string> typed, ListingDto pro) =>
        typed.Count == 0 ? 0 : FeedbackMatch.Score(typed, FeedbackMatch.Words(pro.OtherWork));

    /// <summary>The bonus for the kinds of badge the worker holds (each kind once).</summary>
    public static double TrustFeature(ListingDto pro)
    {
        var kinds = (pro.Badges ?? []).Select(b => b.Kind).ToHashSet();
        return (kinds.Contains(VerificationKinds.Id) ? IdBadgeWeight : 0)
            + (kinds.Contains(VerificationKinds.Tesda) ? TesdaBadgeWeight : 0)
            + (kinds.Contains(VerificationKinds.Developer) ? ProjectBadgeWeight : 0)
            + (kinds.Contains(VerificationKinds.Clearance) ? ClearanceBadgeWeight : 0);
    }

    /// <summary>The bonus for work photos: none 0, one half of <see cref="WorkPhotosWeight"/>, <see cref="FullPhotoBonusAt"/> or more all of it.</summary>
    public static double PhotosFeature(ListingDto pro) =>
        WorkPhotosWeight * Math.Min(pro.WorkPhotos?.Count ?? 0, FullPhotoBonusAt) / FullPhotoBonusAt;

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

    /// <summary>
    /// Cheaper scores more: from all of it at ₱0 down to (1 - <see cref="CheapnessShare"/>) at the reference
    /// (<see cref="RankScales.PriceReference"/>). Over the top of a budget, the rest goes in step with how far over;
    /// over a guide only, it stays at (1 - share). Nothing to compare with: all of it.
    /// </summary>
    private static double PriceFeature(ListingDto pro, RankRequest request, RankScales scales)
    {
        if (scales.PriceReference(pro, request) is not { } reference)
        {
            return 1;
        }

        var share = pro.RatePesos / (double)reference.Pesos;
        return share <= 1 ? 1 - (CheapnessShare * share)
            : reference.OverLoses ? (1 - CheapnessShare) * Clamp(2 - share)
            : 1 - CheapnessShare;
    }

    /// <summary>Unknown experience counts as the least.</summary>
    private static double ExperienceFeature(ListingDto pro, RankScales scales) =>
        scales.ExperienceIds.Count < 2 ? 0 : Math.Max(0, Position(scales.ExperienceIds, pro.ExperienceId)) / (double)(scales.ExperienceIds.Count - 1);

    private static SearchHitDto Hit(ListingDto pro, RankRequest request) => new(
        pro,
        OffersService: request.ServiceId is not null && pro.ServiceIds.Contains(request.ServiceId),
        ServesTown: request.CityId is not null && pro.CityIds.Contains(request.CityId),
        FitsBudget: BudgetRange.TryParse(request.BudgetId, out _, out var max) && (max is null || pro.RatePesos <= max),
        AboveBudget: BudgetRange.TryParse(request.BudgetId, out _, out var top) && top is { } limit && pro.RatePesos > limit);

    private static double Clamp(double value) => Math.Clamp(value, 0, 1);

    /// <summary>Position on a config list, or -1 for an id that isn't on it (unanswered).</summary>
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
