using SideGigPh.Maui.Components.Professional;

namespace SideGigPh.Maui.Services;

/// <summary>
/// SideGigPh's <see cref="IAuthLanding"/>: a user with a Professional profile goes to their home;
/// anyone else goes back to <c>/</c>, where <c>LaunchRouter</c> resumes their onboarding (role,
/// then the interview).
/// </summary>
public sealed class SideGigPhAuthLanding(IProfessionalProfileRepository profiles) : IAuthLanding
{
    public async Task<string> GetDestinationAsync(AuthUser user) =>
        await profiles.HasProfileAsync(user.Uid) ? ProRoutes.Home : "/";
}
