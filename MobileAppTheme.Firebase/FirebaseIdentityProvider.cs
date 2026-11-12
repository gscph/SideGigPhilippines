using Microsoft.Extensions.Logging;
using MobileAppTheme.Auth;
using Plugin.Firebase.Auth;
using Plugin.Firebase.Auth.Google;

namespace MobileAppTheme.Firebase;

/// <summary>
/// Firebase Authentication behind the skeleton's <see cref="IIdentityProvider"/>, via the native
/// SDK (Plugin.Firebase 3.1.x): phone SMS code, Google, adding a verified phone to a Google
/// account (same uid) and saving the user's name. Registered by
/// <see cref="FirebaseAuthSetup.AddFirebaseAuth"/>.
///
/// Error policy: every failure is logged in full (type + stack, to Logcat / Output) and returned
/// as a provider-neutral <see cref="AuthError"/> (see <see cref="FirebaseAuthErrors"/>), with the
/// raw text in <see cref="AuthResult.Detail"/>. Phone code sending logs, then rethrows.
///
/// Needs, in the app: <c>Platforms/Android/google-services.json</c> (GoogleServicesJson) and/or
/// <c>Platforms/iOS/GoogleService-Info.plist</c> (BundleResource), the debug/release SHA-1 and
/// SHA-256 registered in the Firebase console, and on Android
/// <see cref="FirebaseAuthSetup.HandleActivityResult"/> called from <c>MainActivity.OnActivityResult</c>.
/// </summary>
public sealed class FirebaseIdentityProvider(ILogger<FirebaseIdentityProvider> logger) : IIdentityProvider
{
    private static IFirebaseAuth Auth => CrossFirebaseAuth.Current;

    public AuthMethods Methods => AuthMethods.Phone | AuthMethods.Google;

    public async Task<PhoneVerification> SignUpWithPhoneAsync(string phoneNumber)
    {
        try
        {
            // Plugin.Firebase tracks the verification itself; VerifyPhoneOtpAsync only needs the code.
            await Auth.VerifyPhoneNumberAsync(phoneNumber);
            return new PhoneVerification { VerificationId = string.Empty };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Firebase could not send the SMS code.");
            throw;
        }
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
            logger.LogError(ex, "Firebase phone sign-in failed.");
            return AuthResult.Fail(FirebaseAuthErrors.FromPhone(ex), ex.Message);
        }
    }

    public async Task<AuthResult> LinkPhoneAsync(string verificationId, string userEnteredCode)
    {
        try
        {
            var user = await Auth.LinkWithPhoneNumberVerificationCodeAsync(userEnteredCode);

            // The cached ID token doesn't carry the new phone_number claim until it's refreshed,
            // and a backend may read the claim from the token.
            await user.GetIdTokenResultAsync(forceRefresh: true);
            return AuthResult.Ok(Map(user));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Firebase could not add the phone number to the account.");
            return AuthResult.Fail(FirebaseAuthErrors.FromPhone(ex), ex.Message);
        }
    }

    public async Task<AuthResult> SetDisplayNameAsync(string displayName)
    {
        try
        {
            var user = Auth.CurrentUser ?? throw new InvalidOperationException("Nobody is signed in.");
            await user.UpdateProfileAsync(displayName, user.PhotoUrl);

            // The cached user object may still hold the old name.
            return AuthResult.Ok(Map(user, displayName));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Firebase could not save the user's name.");
            return AuthResult.Fail(FirebaseAuthErrors.FromPhone(ex), ex.Message);
        }
    }

    public async Task<AuthResult> SignInWithGoogleAsync()
    {
        try
        {
            // Google sign-in lives on its own cross instance and needs
            // FirebaseAuthGoogleImplementation.Initialize(...) at startup (FirebaseAuthSetup).
            FirebaseAuthSetup.GoogleSignInPending = true;
            var user = await CrossFirebaseAuthGoogle.Current.SignInWithGoogleAsync();
            return AuthResult.Ok(Map(user));
        }
        catch (Exception ex)
        {
            var error = FirebaseAuthErrors.FromGoogle(ex, out var status);
            logger.LogError(ex, "Google sign-in failed: {Status}.", status);
            return AuthResult.Fail(error, status);
        }
        finally
        {
            FirebaseAuthSetup.GoogleSignInPending = false;
        }
    }

    public Task<AuthUser?> GetCurrentUserAsync()
    {
        try
        {
            var user = Auth.CurrentUser;
            return Task.FromResult(user is null ? null : Map(user));
        }
        catch (Exception ex)
        {
            // Firebase isn't initialized (e.g. config file missing): treat as signed out.
            logger.LogWarning(ex, "Firebase is not initialized; treating the user as signed out.");
            return Task.FromResult<AuthUser?>(null);
        }
    }

    public async Task<string?> GetIdTokenAsync()
    {
        try
        {
            // Not forced: the SDK hands back the cached token and refreshes it once it has expired.
            var result = Auth.CurrentUser is { } user ? await user.GetIdTokenResultAsync(forceRefresh: false) : null;
            return result?.Token;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Firebase could not give an ID token.");
            return null;
        }
    }

    public Task SignOutAsync() => Auth.SignOutAsync();

    private static AuthUser Map(IFirebaseUser user, string? displayName = null) => new()
    {
        Uid = user.Uid,
        // IFirebaseUser has no phone property; the phone provider's entry carries the verified number.
        PhoneNumber = user.ProviderInfos?
            .Select(info => info.PhoneNumber)
            .FirstOrDefault(number => !string.IsNullOrEmpty(number)),
        DisplayName = displayName ?? user.DisplayName,
        Email = user.Email,
        PhotoUrl = user.PhotoUrl,
    };
}
