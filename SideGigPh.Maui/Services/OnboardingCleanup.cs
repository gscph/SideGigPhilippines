using SideGigPh.Contracts;
using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services;

/// <summary>
/// Fits saved answers to what we offer now. Answers saved earlier can hold a town or area that is gone
/// (<see cref="DropUnservedTowns"/>), a barangay that was retired or is not in their town (<see cref="DropUnservedBarangays"/>), a trade that moved under another (<see cref="MoveMergedTrades"/>: kept, on its new
/// trade), a trade or job that was retired (<see cref="DropRetiredJobs"/>) or a rate on the
/// old price scale (<see cref="DropOldRate"/>): those are dropped, so the interview asks the question again instead
/// of showing something we no longer offer.
/// </summary>
public static class OnboardingCleanup
{
    /// <summary>Every cleanup, as the saved answers load.</summary>
    public static OnboardingState FitToCatalog(this OnboardingState state) => state.DropUnservedTowns().DropUnservedBarangays().MoveMergedTrades().DropRetiredJobs().DropOldRate();

    /// <summary>
    /// Trades retired by moving them under another trade as one of its jobs (ids are stable, so the old ids stay
    /// here): Plumber, Welder, Roofing &amp; Gutter, Electrician, Carpentry and Masonry are jobs of Construction
    /// (019-schema-construction-trade.sql, 020-schema-construction-roofing.sql and 022-schema-construction-trades.sql
    /// moved the server's rows).
    /// </summary>
    private static readonly IReadOnlyDictionary<string, (string CategoryId, string ServiceId)> MergedTrades =
        new Dictionary<string, (string, string)>
        {
            ["plumbing"] = ("construction", "construction-plumbing"),
            ["welder"] = ("construction", "construction-welding"),
            ["roofing"] = ("construction", "construction-roofing"),
            ["electrical"] = ("construction", "construction-electrical"),
            ["carpentry"] = ("construction", "construction-carpentry"),
            ["masonry_tiling"] = ("construction", "construction-masonry"),
        };

    /// <summary>
    /// Jobs of a merged trade that go to another job than the trade's own (<see cref="MergedTrades"/>): Masonry's tiles
    /// work is Construction's tile setting, as 022-schema-construction-trades.sql moved it on the server.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, string> MergedJobs = new Dictionary<string, string>
    {
        ["masonry-tiles"] = "construction-tiles",
    };

    /// <summary>
    /// An answer naming a merged trade (<see cref="MergedTrades"/>) moves to the trade that took it in, with its job,
    /// instead of being dropped, so a worker or household isn't asked again. Only while that trade is offered.
    /// </summary>
    public static OnboardingState MoveMergedTrades(this OnboardingState state)
    {
        var seeker = state.Seeker;
        if (MergedInto(seeker.CategoryId) is var (seekerTrade, seekerJob))
        {
            seeker.CategoryId = seekerTrade;
            seeker.ServiceId = seeker.ServiceId is null ? null : MergedJobs.GetValueOrDefault(seeker.ServiceId, seekerJob);
            seeker.OtherJobText = null;
        }

        var pro = state.Professional;
        if (MergedInto(pro.CategoryId) is var (proTrade, proJob))
        {
            pro.CategoryId = proTrade;
            var jobs = pro.ServiceIds.Select(job => MergedJobs.GetValueOrDefault(job, proJob)).Distinct().ToList();
            pro.ServiceIds.Clear();
            pro.ServiceIds.AddRange(jobs);

            pro.OtherJobText = null;
        }

        return state;
    }

    private static (string CategoryId, string ServiceId)? MergedInto(string? categoryId) =>
        categoryId is not null && MergedTrades.TryGetValue(categoryId, out var target) && ReferenceData.FindTrade(target.CategoryId) is not null
            ? target
            : null;

    /// <summary>
    /// A Skilled Worker rate from before price guides (an old one-scale tier such as <c>500-1000</c>) is dropped, so the
    /// interview asks the rate again on today's per-job prices (their own from–to range, a
    /// <see cref="ReferenceData.RateLevels"/> point, or "depends on the job"). Seeker budgets need nothing: old tiers read as peso ranges.
    /// </summary>
    public static OnboardingState DropOldRate(this OnboardingState state)
    {
        var pro = state.Professional;
        if (pro.RateTierId is { } rate && rate != ReferenceData.Quote && ReferenceData.Find(ReferenceData.RateLevels, rate) is null
            && !RateRange.TryParse(rate, out _, out _))
        {
            pro.RateTierId = null;
        }

        return state;
    }

    public static OnboardingState DropUnservedTowns(this OnboardingState state)
    {
        var seeker = state.Seeker;
        if (ReferenceData.AreaOf(seeker.CityId) is null)
        {
            seeker.CityId = null;
        }

        seeker.AreaId = ReferenceData.AreaOf(seeker.CityId);

        var pro = state.Professional;
        pro.CityIds.RemoveAll(cityId => ReferenceData.AreaOf(cityId) is null);
        pro.AreaId = ReferenceData.AreaOf(pro.CityIds.FirstOrDefault());
        return state;
    }

    /// <summary>
    /// A household's barangay we no longer serve (retired in <c>004-config.sql</c>) or that isn't in their town is
    /// dropped. It was optional, so nothing is asked again: their next post just starts at "Not sure".
    /// </summary>
    public static OnboardingState DropUnservedBarangays(this OnboardingState state)
    {
        var seeker = state.Seeker;
        seeker.BarangayId = ReferenceData.FindBarangay(seeker.CityId, seeker.BarangayId)?.Id;
        return state;
    }

    /// <summary>
    /// A trade that is no longer offered (<see cref="ReferenceData.Categories"/>) is dropped with its jobs, and a job
    /// the trade no longer has is dropped on its own. The "Others" text goes when its job does.
    /// </summary>
    public static OnboardingState DropRetiredJobs(this OnboardingState state)
    {
        var seeker = state.Seeker;
        if (seeker.CategoryId is not null && !IsOffered(seeker.CategoryId))
        {
            seeker.CategoryId = null;
        }

        if (seeker.ServiceId is not null && !HasJob(seeker.CategoryId, seeker.ServiceId))
        {
            seeker.ServiceId = null;
        }

        if (!ReferenceData.IsOtherJob(seeker.ServiceId))
        {
            seeker.OtherJobText = null;
        }

        var pro = state.Professional;
        if (pro.CategoryId is not null && !IsOffered(pro.CategoryId))
        {
            pro.CategoryId = null;
        }

        pro.ServiceIds.RemoveAll(serviceId => !HasJob(pro.CategoryId, serviceId));
        if (!pro.ServiceIds.Any(ReferenceData.IsOtherJob))
        {
            pro.OtherJobText = null;
        }

        return state;
    }

    // "Others" (the tile after the trades) stays: it is an answer, not a trade we could retire.
    private static bool IsOffered(string categoryId) => ReferenceData.FindTrade(categoryId) is not null;

    private static bool HasJob(string? categoryId, string serviceId) =>
        ReferenceData.ServicesOf(categoryId).Any(service => service.Id == serviceId);
}
