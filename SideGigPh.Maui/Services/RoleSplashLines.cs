using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Services;

/// <summary>
/// What the busy splash says under the Tarbaho logo, by the user's role: a Seeker sees the brand line with the
/// honorific taking turns ("Find Manongs nearby" → "Find Ates nearby"…), a Skilled Worker sees why the app is worth
/// it for them (jobs near you, no commission). Before a role is picked it is the Seeker line, like the launch splash.
/// </summary>
public sealed class RoleSplashLines(IOnboardingStore store) : ISplashLines
{
    public async Task<IReadOnlyList<LocalizedText>> LinesAsync()
    {
        var state = await store.LoadAsync();
        return For(state.Role);
    }

    public static IReadOnlyList<LocalizedText> For(UserRole? role) => role == UserRole.Professional ? ProLines : SeekerLines;

    /// <summary>
    /// The Seeker tagline once per honorific (plural in English, as on the launch splash), with the trust line
    /// (<see cref="Copy.TrustTagline"/>) second, so even a short wait shows it.
    /// </summary>
    public static IReadOnlyList<LocalizedText> SeekerLines { get; } = [.. Honorifics.All.Select(SeekerLine).Take(1), Copy.TrustTagline, .. Honorifics.All.Select(SeekerLine).Skip(1)];

    public static IReadOnlyList<LocalizedText> ProLines { get; } = Copy.ProTaglines;

    /// <summary>The Seeker tagline with one honorific, in both languages.</summary>
    public static LocalizedText SeekerLine(string honorific) => new(
        string.Format(Copy.SeekerTagline.English, Honorifics.Plural(honorific)),
        string.Format(Copy.SeekerTagline.Taglish, honorific));
}
