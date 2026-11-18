using Microsoft.Extensions.Logging;
using MobileAppTheme.Auth;
using Plugin.Firebase.Auth;
using Plugin.Firebase.Auth.Google;
using Plugin.Firebase.Core.Exceptions;

namespace MobileAppTheme.Firebase;

/// <summary>
/// Firebase Authentication behind the skeleton's <see cref="IIdentityProvider"/>, via the native
/// SDK (Plugin.Firebase 3.1.x): phone SMS code, Google, Sign in with Apple (iOS only), adding a verified phone to a Google
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
    /// <summary>
    /// The longest sending an SMS code may take (Firebase checks the app first, sometimes with a reCAPTCHA page).
    /// Past it the person sees "couldn't send" instead of a spinner that never stops.
    /// </summary>
    public static readonly TimeSpan SendCodeLimit = TimeSpan.FromSeconds(90);

    /// <summary>
    /// The longest getting the sign-in token may take. Every API call waits for it, so past this the call goes out
    /// without one (and fails fast) instead of freezing the screen that waits for it.
    /// </summary>
    public static readonly TimeSpan TokenLimit = TimeSpan.FromSeconds(15);

    /// <summary>
    /// The longest checking a typed SMS code (or linking the number) may take. A connection that drops at that moment
    /// used to leave the button spinning for good, with "use another number" disabled: now it says "check your signal".
    /// </summary>
    public static readonly TimeSpan VerifyCodeLimit = TimeSpan.FromSeconds(30);

    private static IFirebaseAuth Auth => CrossFirebaseAuth.Current;

    /// <summary>The number the last code went to (E.164), to recognise an automatic sign-in with it.</summary>
    private string? _pendingPhone;

    /// <summary>
    /// Phone and Google everywhere; Apple on iOS only (App Store 4.8 asks for it next to Google; Android doesn't need
    /// it). Apple also needs the Sign in with Apple capability on the App ID, the
    /// <c>com.apple.developer.applesignin</c> entitlement and the Apple provider turned on in Firebase.
    /// </summary>
    public AuthMethods Methods => OperatingSystem.IsIOS()
        ? AuthMethods.Phone | AuthMethods.Google | AuthMethods.Apple
        : AuthMethods.Phone | AuthMethods.Google;

    public async Task<PhoneVerification> SignUpWithPhoneAsync(string phoneNumber)
    {
        try
        {
            // Plugin.Firebase tracks the verification itself; VerifyPhoneOtpAsync only needs the code.
            _pendingPhone = phoneNumber;
            await Auth.VerifyPhoneNumberAsync(phoneNumber).WaitAsync(SendCodeLimit);
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
            var user = await Auth.SignInWithPhoneNumberVerificationCodeAsync(userEnteredCode).WaitAsync(VerifyCodeLimit);
            return AuthResult.Ok(Map(user));
        }
        catch (Exception ex) when (Auth.CurrentUser is { } signedIn && Map(signedIn).PhoneNumber is { } number && number == _pendingPhone)
        {
            // Android read the SMS and signed in by itself (instant verification), so the typed code was refused as used:
            // the person is in with the number the code went to.
            logger.LogWarning(ex, "The typed code was refused, but the number was already confirmed automatically.");
            return AuthResult.Ok(Map(signedIn));
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
#if ANDROID
            // The number may already be on another account (often the person's own earlier phone sign-in): Firebase's
            // SDK then signs in to it with this same code, never a second one (AndroidPhoneLink).
            if (AndroidPhoneLink.PendingVerificationId(Auth) is { Length: > 0 } sessionId)
            {
                var outcome = await AndroidPhoneLink.LinkOrSignInAsync(sessionId, userEnteredCode).WaitAsync(VerifyCodeLimit);
                var current = Auth.CurrentUser ?? throw new InvalidOperationException("Nobody is signed in after adding the number.");
                await current.GetIdTokenResultAsync(forceRefresh: true).WaitAsync(TokenLimit);
                if (outcome == AndroidPhoneLink.Outcome.SwitchedAccount)
                {
                    logger.LogInformation("The number belonged to another account; signed in to it with the same code.");
                    return AuthResult.SwitchedTo(Map(current));
                }

                return AuthResult.Ok(Map(current));
            }

            logger.LogWarning("The SMS session id couldn't be read from the plugin; adding the number through the plugin.");
#endif
            var user = await Auth.LinkWithPhoneNumberVerificationCodeAsync(userEnteredCode).WaitAsync(VerifyCodeLimit);

            // The cached ID token doesn't carry the new phone_number claim until it's refreshed,
            // and a backend may read the claim from the token.
            await user.GetIdTokenResultAsync(forceRefresh: true).WaitAsync(TokenLimit);
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

    public async Task<AuthResult> SignInWithAppleAsync()
    {
        try
        {
            // Apple shares the name only the first time; with none, the sign-in sheet asks for it (NamePage).
            var user = await Auth.SignInWithAppleAsync();
            return AuthResult.Ok(Map(user));
        }
        catch (Exception ex)
        {
            var error = FirebaseAuthErrors.FromApple(ex);
            logger.LogError(ex, "Sign in with Apple failed: {Error}.", error);
            return AuthResult.Fail(error, ex.Message);
        }
    }

    public async Task DeleteAccountAsync()
    {
        try
        {
            // Usually the server already deleted it (Firebase Admin); then this fails with "user not found", which is fine.
            if (Auth.CurrentUser is { } user)
            {
                await user.DeleteAsync();
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Firebase could not delete the account on the phone (the server may have already).");
        }

        await SignOutQuietlyAsync();
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
            var result = Auth.CurrentUser is { } user ? await user.GetIdTokenResultAsync(forceRefresh: false).WaitAsync(TokenLimit) : null;
            return result?.Token;
        }
        catch (TimeoutException)
        {
            logger.LogError("Firebase gave no ID token within {Seconds} s.", TokenLimit.TotalSeconds);
            return null;
        }
        catch (Exception ex) when (IsDeadSession(ex))
        {
            // The account was deleted or disabled, or its session revoked: nothing will work until they sign in again.
            // Staying "signed in" would send every call without a token (401) and every error report too.
            logger.LogWarning(ex, "Firebase session is no longer valid; signing out.");
            await SignOutQuietlyAsync();
            return null;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Firebase could not give an ID token.");
            return null;
        }
    }

    public Task SignOutAsync() => Auth.SignOutAsync();

    /// <summary>
    /// True when the account behind the cached session is gone (deleted, disabled) or its session was revoked. The
    /// plugin reports that as <see cref="FirebaseAuthException"/> on iOS, but on Android the native SDK's
    /// <c>FirebaseAuthInvalidUserException</c> (deleted / disabled) comes through unwrapped, so its type name is
    /// checked down the inner exceptions too.
    /// </summary>
    internal static bool IsDeadSession(Exception ex)
    {
        for (var e = ex; e is not null; e = e.InnerException)
        {
            if (e is FirebaseAuthException { Reason: FIRAuthError.UserTokenExpired or FIRAuthError.UserNotFound or FIRAuthError.UserDisabled }
                || e.GetType().Name is "FirebaseAuthInvalidUserException")
            {
                return true;
            }
        }

        return false;
    }

    private async Task SignOutQuietlyAsync()
    {
        try
        {
            await Auth.SignOutAsync();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Firebase could not sign out the dead session.");
        }
    }

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
