namespace MobileAppTheme.Auth;

/// <summary>
/// The authentication port. The sign-in screens and the app talk only to this interface and to
/// the provider-neutral <see cref="AuthUser"/> / <see cref="AuthResult"/>; a vendor (Firebase,
/// Supabase, Azure AD B2C, Auth0…) is an adapter in its own project. Switching providers means
/// registering a different adapter in <c>MauiProgram</c>, nothing else.
/// </summary>
public interface IIdentityProvider
{
    /// <summary>The sign-in methods this provider offers; the sign-in screen shows only these.</summary>
    AuthMethods Methods { get; }

    /// <summary>
    /// Starts phone sign-in by sending an SMS code to <paramref name="phoneNumber"/> (E.164, e.g.
    /// <c>+639171234567</c>). Returns the handle for <see cref="VerifyPhoneOtpAsync"/>. Throws when
    /// the code can't be sent; the adapter logs the full exception first.
    /// </summary>
    Task<PhoneVerification> SignUpWithPhoneAsync(string phoneNumber);

    /// <summary>Confirms the code the user typed and signs them in.</summary>
    Task<AuthResult> VerifyPhoneOtpAsync(string verificationId, string userEnteredCode);

    /// <summary>Interactive Google sign-in.</summary>
    Task<AuthResult> SignInWithGoogleAsync();

    /// <summary>The signed-in user, or null (also when the provider isn't set up).</summary>
    Task<AuthUser?> GetCurrentUserAsync();

    Task SignOutAsync();
}
