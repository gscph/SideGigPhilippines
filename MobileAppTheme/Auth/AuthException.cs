using MobileAppTheme.Services;

namespace MobileAppTheme.Auth;

/// <summary>
/// A sign-in step that answers by throwing (<see cref="IIdentityProvider.SignUpWithPhoneAsync"/>) failed for a known
/// <see cref="Error"/>: the adapter maps its vendor's exception to it, so the screens never see a vendor type.
/// </summary>
public sealed class AuthException(AuthError error, string message, Exception? inner = null) : Exception(message, inner)
{
    public AuthError Error { get; } = error;

    /// <summary>
    /// The <see cref="AuthError"/> for any failure of a throwing sign-in step: the adapter's own mapping, a time limit
    /// that passed or a request that got no answer (<see cref="NetworkErrors.IsNetwork"/>) is
    /// <see cref="AuthError.Network"/>, anything else <see cref="AuthError.Unknown"/>.
    /// </summary>
    public static AuthError ErrorOf(Exception failure) => failure switch
    {
        AuthException known => known.Error,
        _ when NetworkErrors.IsNetwork(failure) => AuthError.Network,
        _ => AuthError.Unknown,
    };
}
