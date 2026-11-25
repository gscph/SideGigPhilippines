using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Maui.LifecycleEvents;
using MobileAppTheme.Auth;
using Plugin.Firebase.Auth.Google;
#if ANDROID
using Android.App;
using Android.Content;
#elif IOS
using Foundation;
#endif

namespace MobileAppTheme.Firebase;

/// <summary>
/// One call in <c>MauiProgram</c> switches the app's sign-in to Firebase:
/// <c>builder.AddFirebaseAuth(googleWebClientId)</c>. It initializes the native SDK during platform
/// startup and registers <see cref="FirebaseIdentityProvider"/> as the <see cref="IIdentityProvider"/>.
/// </summary>
public static class FirebaseAuthSetup
{
    /// <param name="googleWebClientId">
    /// The web OAuth client (<c>"client_type": 3</c>) from <c>google-services.json</c>; Android needs
    /// it to get the Google ID token.
    /// </param>
    public static MauiAppBuilder AddFirebaseAuth(this MauiAppBuilder builder, string googleWebClientId)
    {
        builder.ConfigureLifecycleEvents(events =>
        {
#if ANDROID
            events.AddAndroid(android => android.OnCreate((activity, _) =>
            {
                FirebaseCore.Start(activity);
                FirebaseCore.TryRun("Google sign-in", () => FirebaseAuthGoogleImplementation.Initialize(googleWebClientId));
                FirebaseCore.TryRun("Emulator phone check", UseRecaptchaOnEmulator);
            }).OnStart(activity => FirebaseCore.TryRun("Phone code send", () => AndroidPhoneVerification.ScreenBack(activity))));
#elif IOS
            events.AddiOS(ios => ios
                .WillFinishLaunching((_, __) =>
                {
                    FirebaseCore.Start();
                    FirebaseCore.TryRun("Google sign-in", FirebaseAuthGoogleImplementation.Initialize);
                    return false;
                })
                // Phone sign-in's web check (reCAPTCHA) comes back through the Encoded App ID URL scheme. MAUI's iOS app
                // is scene-based, so the URL reaches the scene, which Firebase's app-delegate hooks may never see: handed
                // over here, sending the code can't wait for it until it times out.
                .SceneOpenUrl((_, contexts) => contexts.ToArray().Any(context => HandleUrl(context.Url)))
                .OpenUrl((_, url, _) => HandleUrl(url)));
#endif
        });

        // Replace, not TryAdd: the skeleton registers NoIdentityProvider as the default.
        builder.Services.Replace(ServiceDescriptor.Singleton<IIdentityProvider, FirebaseIdentityProvider>());
        return builder;
    }

#if ANDROID
    /// <summary>
    /// Phone sign-in proves the app is genuine with Play Integrity. On an emulator Play can't vouch for an app it
    /// didn't install, so Firebase refuses to send the code (17028 "Invalid app info in play_integrity_token") instead
    /// of falling back. On an emulator, use the reCAPTCHA check (a browser tab, needs the SHA-1) instead. Real phones
    /// keep Play Integrity.
    /// </summary>
    private static void UseRecaptchaOnEmulator()
    {
        if (DeviceInfo.Current.DeviceType == DeviceType.Virtual)
        {
            global::Firebase.Auth.FirebaseAuth.Instance.FirebaseAuthSettings.ForceRecaptchaFlowForTesting(true);
        }
    }

    /// <summary>
    /// Call from <c>MainActivity.OnActivityResult</c> (after <c>base</c>). Google sign-in returns its
    /// result through the activity; without forwarding it, <c>SignInWithGoogleAsync</c> never completes
    /// (no exception, stuck on "Connecting…"). Results of other activities (the photo picker, the
    /// dialer…) arrive here too and are ignored: the plugin throws a NullReferenceException when no
    /// Google sign-in is waiting, and an exception here kills the app.
    /// </summary>
    public static void HandleActivityResult(int requestCode, Result resultCode, Intent? data)
    {
        if (!GoogleSignInPending)
        {
            return;
        }

        try
        {
            _ = FirebaseAuthGoogleImplementation.HandleActivityResultAsync(requestCode, resultCode, data);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Firebase] activity result not handled: {ex}");
        }
    }
#endif

#if IOS
    /// <summary>
    /// Call from <c>AppDelegate.RegisteredForRemoteNotifications</c>. Firebase proves phone sign-in comes from this app
    /// with a silent push; it needs Apple's push token for that, or it falls back to the slower web check.
    /// </summary>
    public static void HandleApnsToken(NSData deviceToken)
    {
        if (FirebaseCore.IsRunning)
        {
            global::Firebase.Auth.Auth.DefaultInstance?.SetApnsToken(deviceToken, global::Firebase.Auth.AuthApnsTokenType.Unknown);
        }
    }

    /// <summary>
    /// Call from <c>AppDelegate.DidReceiveRemoteNotification</c>; true when it was Firebase's silent phone-sign-in push.
    /// </summary>
    public static bool HandleRemoteNotification(NSDictionary userInfo) =>
        FirebaseCore.IsRunning && global::Firebase.Auth.Auth.DefaultInstance?.CanHandleNotification(userInfo) == true;

    private static bool HandleUrl(NSUrl? url) =>
        url is not null && FirebaseCore.IsRunning && global::Firebase.Auth.Auth.DefaultInstance?.CanHandleUrl(url) == true;
#endif

    /// <summary>True while a Google sign-in is waiting for the account picker's result.</summary>
    internal static volatile bool GoogleSignInPending;
}
