#if IOS
using System.Security.Cryptography;
using System.Text;
using AuthenticationServices;
using Firebase.Auth;
using Foundation;
using Microsoft.Maui.ApplicationModel;
using UIKit;

namespace MobileAppTheme.Firebase;

/// <summary>
/// Sign in with Apple through Firebase's own SDK, the way Firebase documents it for Apple platforms: Apple's sheet is
/// asked with the SHA-256 of a one-time nonce, and Firebase gets Apple's ID token with the raw nonce (and the name,
/// which Apple shares only the first time). Plugin.Firebase 3.1.x sent the token with no nonce at all, which Firebase
/// refuses, and its "cancel" came back as an error with no "cancel" in its text.
/// </summary>
internal static class IosAppleSignIn
{
    /// <summary>Apple's code for the person closing the sheet (ASAuthorizationError.Canceled).</summary>
    private const long CanceledCode = 1001;

    /// <summary>Signs in to Firebase with Apple. Throws <see cref="OperationCanceledException"/> when the person closed the sheet.</summary>
    public static async Task SignInAsync()
    {
        var rawNonce = NewNonce();
        var apple = await MainThread.InvokeOnMainThreadAsync(() => AskAppleAsync(Sha256(rawNonce)));

        var idToken = apple.IdentityToken is { } token ? new NSString(token, NSStringEncoding.UTF8).ToString() : null;
        if (string.IsNullOrEmpty(idToken))
        {
            throw new InvalidOperationException("Apple gave no ID token.");
        }

        var credential = OAuthProvider.GetAppleCredential(idToken, rawNonce, apple.FullName);
        var auth = global::Firebase.Auth.Auth.DefaultInstance ?? throw new InvalidOperationException("Firebase is not running.");
        await auth.SignInWithCredentialAsync(credential);
    }

    private static Task<ASAuthorizationAppleIdCredential> AskAppleAsync(string hashedNonce)
    {
        var request = new ASAuthorizationAppleIdProvider().CreateRequest();
        request.RequestedScopes = [ASAuthorizationScope.FullName, ASAuthorizationScope.Email];
        request.Nonce = hashedNonce;

        var sheet = new AppleSheet(Platform.GetCurrentUIViewController()?.View?.Window
            ?? throw new InvalidOperationException("There is no window to show Sign in with Apple on."));
        var controller = new ASAuthorizationController([request])
        {
            Delegate = sheet,
            PresentationContextProvider = sheet,
        };
        controller.PerformRequests();

        // The controller holds its delegate weakly: keep both alive until Apple answers.
        return sheet.Answer.Task.ContinueWith(answer => { GC.KeepAlive(controller); GC.KeepAlive(sheet); return answer; })
            .Unwrap();
    }

    private static string NewNonce() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static string Sha256(string text) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text)));

    private sealed class AppleSheet(UIWindow window) : NSObject, IASAuthorizationControllerDelegate, IASAuthorizationControllerPresentationContextProviding
    {
        public TaskCompletionSource<ASAuthorizationAppleIdCredential> Answer { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        [Export("presentationAnchorForAuthorizationController:")]
        public UIWindow GetPresentationAnchor(ASAuthorizationController controller) => window;

        [Export("authorizationController:didCompleteWithAuthorization:")]
        public void DidComplete(ASAuthorizationController controller, ASAuthorization authorization)
        {
            if (authorization.GetCredential<ASAuthorizationAppleIdCredential>() is { } credential)
            {
                Answer.TrySetResult(credential);
            }
            else
            {
                Answer.TrySetException(new InvalidOperationException("Apple answered without an Apple ID credential."));
            }
        }

        [Export("authorizationController:didCompleteWithError:")]
        public void DidComplete(ASAuthorizationController controller, NSError error)
        {
            if (error.Code == CanceledCode)
            {
                Answer.TrySetCanceled();
            }
            else
            {
                Answer.TrySetException(new NSErrorException(error));
            }
        }
    }
}
#endif
