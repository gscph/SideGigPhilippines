using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services;

/// <summary>
/// Local-first persistence for the onboarding interview; no account required.
/// Today a JSON file on the device; a server-synced adapter can replace it later.
/// </summary>
public interface IOnboardingStore
{
    /// <summary>The saved state, or a new empty one on first launch. Same instance on every call.</summary>
    Task<OnboardingState> LoadAsync();

    Task SaveAsync(OnboardingState state);
}
