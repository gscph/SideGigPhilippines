using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services;

/// <summary>
/// Authentication port. The UI talks only to this abstraction; Firebase (or any
/// future provider) is an adapter behind it. See CLAUDE.md "Ports &amp; Adapters".
/// </summary>
public interface IIdentityProvider
{
    /// <summary>Starts phone sign-in by sending an SMS OTP. Returns the verification handle.</summary>
    Task<PhoneVerification> SignUpWithPhoneAsync(string phoneNumber);

    /// <summary>Confirms the OTP the user entered and signs them in.</summary>
    Task<AuthResult> VerifyPhoneOtpAsync(string verificationId, string userEnteredCode);

    /// <summary>Interactive Google sign-in.</summary>
    Task<AuthResult> SignInWithGoogleAsync();

    /// <summary>The currently signed-in user, or null.</summary>
    Task<AuthUser?> GetCurrentUserAsync();

    /// <summary>Signs the current user out.</summary>
    Task SignOutAsync();
}
