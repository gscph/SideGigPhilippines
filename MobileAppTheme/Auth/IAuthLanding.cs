namespace MobileAppTheme.Auth;

/// <summary>
/// Where the app sends a user right after they sign in (or open <c>AuthPage</c> already signed
/// in), e.g. their home if they have a profile, else back to onboarding. The app implements it;
/// the default, <see cref="HomeAuthLanding"/>, goes to <c>/</c>.
/// </summary>
public interface IAuthLanding
{
    Task<string> GetDestinationAsync(AuthUser user);
}

/// <summary>Default <see cref="IAuthLanding"/>: back to <c>/</c>, where the app's launch page decides.</summary>
public sealed class HomeAuthLanding : IAuthLanding
{
    public Task<string> GetDestinationAsync(AuthUser user) => Task.FromResult("/");
}
