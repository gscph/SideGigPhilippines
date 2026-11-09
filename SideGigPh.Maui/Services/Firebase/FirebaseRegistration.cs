#if ANDROID || IOS
using Microsoft.Maui.LifecycleEvents;
using Plugin.Firebase.Auth.Google;
#if ANDROID
using Plugin.Firebase.Core.Platforms.Android;
#elif IOS
using Plugin.Firebase.Core.Platforms.iOS;
#endif

namespace SideGigPh.Maui.Services.Firebase;

/// <summary>
/// Initializes the native Firebase SDK during platform startup. Called from
/// MauiProgram (Android/iOS only). Initialization will throw at runtime until the
/// platform config files (google-services.json / GoogleService-Info.plist) are
/// present — expected until the user supplies them.
/// </summary>
public static class FirebaseRegistration
{
    // Web client ID (OAuth client_type 3) from google-services.json. Used by
    // FirebaseAuthGoogleImplementation.Initialize to request the ID token on Android.
    private const string GoogleWebClientId = "742654871295-4slv8fpgpbrllq8aoe3vbiphv3e64q3f.apps.googleusercontent.com";

    public static MauiAppBuilder RegisterFirebaseServices(this MauiAppBuilder builder)
    {
        builder.ConfigureLifecycleEvents(events =>
        {
#if ANDROID
            events.AddAndroid(android => android.OnCreate((activity, _) => TryInitialize(() =>
            {
                CrossFirebase.Initialize(activity);
                FirebaseAuthGoogleImplementation.Initialize(GoogleWebClientId);
            })));
#elif IOS
            events.AddiOS(ios => ios.WillFinishLaunching((_, __) =>
            {
                TryInitialize(() =>
                {
                    CrossFirebase.Initialize();
                    FirebaseAuthGoogleImplementation.Initialize();
                });
                return false;
            }));
#endif
        });

        return builder;
    }

    // Firebase initialization throws if the platform config files are missing. During
    // development (before google-services.json / GoogleService-Info.plist are added) we
    // must not let that crash the app at launch — the auth screen should still load and
    // sign-in attempts fail gracefully instead.
    private static void TryInitialize(Action initialize)
    {
        try
        {
            initialize();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Firebase] initialization skipped: {ex.Message}");
        }
    }
}
#endif
