#if IOS
using Firebase.Auth;
using Foundation;
using UIKit;

namespace MobileAppTheme.Firebase;

/// <summary>
/// Sending an SMS code on iOS through Firebase's own SDK instead of the plugin. Plugin.Firebase 3.1.x shows Firebase's
/// web check (reCAPTCHA, used when Apple's silent push can't prove the app) over
/// <c>UIApplication.KeyWindow</c>, which a scene-based app like MAUI's doesn't reliably have; here it is shown over the
/// screen MAUI says is on top. The page comes back to the app through its Encoded App ID URL scheme, which the scene
/// hands to Firebase (<see cref="FirebaseAuthSetup"/>, <c>SceneOpenUrl</c>), since Firebase's own hooks may not see it.
/// </summary>
internal sealed class IosPhoneVerification
{
    private string? _verificationId;
    private int _sends;

    /// <summary>
    /// Asks Firebase to send a code to <paramref name="phoneNumber"/> (E.164) and waits until it is sent. A late answer
    /// to an older send (one the caller stopped waiting for) never replaces a newer send's session.
    /// </summary>
    public async Task SendAsync(string phoneNumber)
    {
        var send = Interlocked.Increment(ref _sends);
        _verificationId = null;
        var sessionId = await MainThread.InvokeOnMainThreadAsync(() =>
            PhoneAuthProvider.DefaultInstance.VerifyPhoneNumberAsync(phoneNumber, new TopScreen()));
        if (send == Volatile.Read(ref _sends))
        {
            _verificationId = sessionId;
        }
    }

    /// <summary>The credential for the code the person typed. Throws when no code was sent.</summary>
    public PhoneAuthCredential CredentialFor(string code) => _verificationId is { Length: > 0 } sessionId
        ? PhoneAuthProvider.DefaultInstance.GetCredential(sessionId, code)
        : throw new InvalidOperationException("No SMS code was sent in this run of the app.");

    /// <summary>Shows Firebase's web check over whatever screen is on top now.</summary>
    private sealed class TopScreen : NSObject, IAuthUIDelegate
    {
        private UIViewController? _presenter;

        public void PresentViewController(UIViewController viewControllerToPresent, bool flag, Action? completion)
        {
            _presenter = Platform.GetCurrentUIViewController()
                ?? throw new InvalidOperationException("There is no screen to show the phone check on.");
            _presenter.PresentViewController(viewControllerToPresent, flag, completion);
        }

        public void DismissViewController(bool animated, Action? completion)
        {
            if (_presenter is { } presenter)
            {
                presenter.DismissViewController(animated, completion);
            }
            else
            {
                completion?.Invoke();
            }
        }
    }
}
#endif
