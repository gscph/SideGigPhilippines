using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services;

/// <summary>
/// Fits saved answers to the towns we serve now (<see cref="ReferenceData.Cities"/>). Answers saved when coverage
/// was wider can hold a town or area that is gone: the town is dropped and the area follows from what is left,
/// so the interview asks for the town again instead of showing something we no longer cover.
/// </summary>
public static class OnboardingCleanup
{
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
}
