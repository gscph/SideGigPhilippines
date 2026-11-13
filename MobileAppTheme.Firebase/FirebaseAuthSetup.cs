using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Maui.LifecycleEvents;
using MobileAppTheme.Auth;
using Plugin.Firebase.Auth.Google;
#if ANDROID
using Android.App;
using Android.Content;
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
            }));
#elif IOS
            events.AddiOS(ios => ios.WillFinishLaunching((_, __) =>
            {
                FirebaseCore.Start();
                FirebaseCore.TryRun("Google sign-in", FirebaseAuthGoogleImplementation.Initialize);
                return false;
            }));
#endif
        });

        // Replace, not TryAdd: the skeleton registers NoIdentityProvider as the default.
        builder.Services.Replace(ServiceDescriptor.Singleton<IIdentityProvider, FirebaseIdentityProvider>());
        return builder;
    }

#if ANDROID
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

    /// <summary>True while a Google sign-in is waiting for the account picker's result.</summary>
    internal static volatile bool GoogleSignInPending;
}
