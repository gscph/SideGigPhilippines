namespace SideGigPh.Maui.Services.Dev;

/// <summary>
/// Temporary profile lookup until Firestore is wired. Returns "no profile" so a
/// freshly authenticated user is routed to onboarding. Flip <see cref="PretendHasProfile"/>
/// to <c>true</c> to exercise the Professional route during development.
/// </summary>
public sealed class StubProfessionalProfileRepository : IProfessionalProfileRepository
{
    // Dev toggle: true routes to the Professional page, false routes to onboarding.
    private const bool PretendHasProfile = false;

    public Task<bool> HasProfileAsync(string uid) => Task.FromResult(PretendHasProfile);
}
