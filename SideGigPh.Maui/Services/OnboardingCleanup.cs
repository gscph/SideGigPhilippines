using SideGigPh.Contracts;
using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services;

/// <summary>
/// Fits saved answers to what we offer now. Answers saved earlier can hold a town or area that is gone
/// (<see cref="DropUnservedTowns"/>), a trade or job that was retired (<see cref="DropRetiredJobs"/>) or a rate on the
/// old price scale (<see cref="DropOldRate"/>): those are dropped, so the interview asks the question again instead
/// of showing something we no longer offer.
/// </summary>
public static class OnboardingCleanup
{
    /// <summary>Every cleanup, as the saved answers load.</summary>
    public static OnboardingState FitToCatalog(this OnboardingState state) => state.DropUnservedTowns().DropRetiredJobs().DropOldRate();

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
