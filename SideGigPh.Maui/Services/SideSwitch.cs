using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services;

/// <summary>
/// One account can be a household and a Skilled Worker: the Me page of each side switches to the other
/// (<c>/switch/{side}</c>, <c>SwitchSidePage</c>). Each side keeps its own answers and its own finished interview
/// (<see cref="OnboardingState.SeekerFinishedAt"/>, <see cref="OnboardingState.ProFinishedAt"/>), so switching back
/// never asks again; a side never set up starts its interview, with any answers it already has filled in.
/// </summary>
public static class SideSwitch
{
    /// <summary>The page that switches: <c>/switch/seeker</c> or <c>/switch/professional</c>.</summary>
    public static string PathTo(UserRole role) => $"/switch/{role.ToString().ToLowerInvariant()}";

    /// <summary>The side named in <see cref="PathTo"/>'s address, or null for anything else.</summary>
    public static UserRole? Parse(string? side) =>
        Enum.TryParse<UserRole>(side, ignoreCase: true, out var role) && Enum.IsDefined(role) ? role : null;

    public static UserRole Other(this UserRole role) => role == UserRole.Seeker ? UserRole.Professional : UserRole.Seeker;

    /// <summary>
    /// Makes <paramref name="role"/> the side the app opens on and says where to go: that side's home (or, for a worker
    /// not live yet, the step they left) when its interview is finished, else its first question.
    /// </summary>
    /// <summary>
    /// Back from <paramref name="role"/>'s interview: where it would go (<paramref name="path"/>), except that the first
    /// question's Back (the role fork) returns to the other side when that side is set up: someone who tapped "Become a
    /// Skilled Worker" and changed their mind is back on their customer pages, never asked to pick a side again.
    /// </summary>
    public static string BackFrom(OnboardingState state, UserRole role, string path) =>
        path == OnboardingSteps.RolePath && state.HasFinished(role.Other()) ? PathTo(role.Other()) : path;

    public static string MoveTo(OnboardingState state, UserRole role)
    {
        state.Role = role;
        if (state.HasFinished(role))
        {
            state.ResumePath = null;
            return OnboardingSteps.LaunchPath(state);
        }

        state.ResumePath = OnboardingSteps.FirstPath(role);
        return state.ResumePath;
    }
}
