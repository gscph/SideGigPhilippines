#if ANDROID || IOS
using Plugin.Firebase.Auth;
using Plugin.Firebase.Auth.Google;
using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services.Firebase;

/// <summary>
/// Native Firebase Authentication adapter (Android/iOS) behind <see cref="IIdentityProvider"/>.
/// Uses Plugin.Firebase (3.1.x).
///
/// REQUIRES platform config to be present at build/run time:
///   - Android: Platforms/Android/google-services.json   (build action: GoogleServicesJson)
///   - iOS:     Platforms/iOS/GoogleService-Info.plist    (build action: BundleResource)
/// plus a configured Google OAuth client (Android SHA-1; iOS reversed-client-id URL scheme)
/// and, for Google sign-in, FirebaseAuthGoogleImplementation.Initialize(...) at startup.
///
/// This path cannot be run/verified on Windows (the package is Android/iOS only); it is
/// compile-verified against Plugin.Firebase 3.1.3 on net10.0-android.
/// </summary>
public sealed class FirebaseIdentityProvider : IIdentityProvider
{
    private static IFirebaseAuth Auth => CrossFirebaseAuth.Current;

    public async Task<PhoneVerification> SignUpWithPhoneAsync(string phoneNumber)
    {
        // Triggers the SMS/OTP flow. Plugin.Firebase tracks the verification internally;
        // the code is confirmed via VerifyPhoneOtpAsync below.
        await Auth.VerifyPhoneNumberAsync(phoneNumber);
        return new PhoneVerification { VerificationId = string.Empty };
    }

    public async Task<AuthResult> VerifyPhoneOtpAsync(string verificationId, string userEnteredCode)
    {
        try
        {
            var user = await Auth.SignInWithPhoneNumberVerificationCodeAsync(userEnteredCode);
            return AuthResult.Ok(Map(user));
        }
        catch (Exception ex)
        {
            return AuthResult.Fail(ex.Message);
        }
    }

    public async Task<AuthResult> SignInWithGoogleAsync()
    {
        try
        {
            // Google sign-in lives on its own cross instance (Plugin.Firebase.Auth.Google);
            // requires FirebaseAuthGoogleImplementation.Initialize(...) to have run at startup.
            var user = await CrossFirebaseAuthGoogle.Current.SignInWithGoogleAsync();
            return AuthResult.Ok(Map(user));
        }
        catch (Exception ex)
        {
            return AuthResult.Fail(ex.Message);
        }
    }

    public Task<AuthUser?> GetCurrentUserAsync()
    {
        try
        {
            var user = Auth.CurrentUser;
            return Task.FromResult(user is null ? null : Map(user));
        }
        catch
        {
            // Firebase not initialized (e.g. config missing) — treat as signed out.
            return Task.FromResult<AuthUser?>(null);
        }
    }

    public Task SignOutAsync() => Auth.SignOutAsync();

    private static AuthUser Map(IFirebaseUser user) => new()
    {
        Uid = user.Uid,
        DisplayName = user.DisplayName,
        Email = user.Email,
        PhotoUrl = user.PhotoUrl,
    };
}
#endif
