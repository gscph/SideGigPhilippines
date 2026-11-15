using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Maui.LifecycleEvents;
using MobileAppTheme.Services;

namespace MobileAppTheme.Firebase;

/// <summary>
/// One call in <c>MauiProgram</c> turns on Firebase A/B Testing: <c>builder.AddFirebaseExperiments()</c>, behind the
/// skeleton's <see cref="IExperiments"/>. A/B Testing hands its variants out through Remote Config and measures them
/// with Analytics, so it wants <c>AddFirebaseAnalytics</c> on too. It is a build-time feature flag: the call only does
/// something in a build made with <c>FirebaseExperiments=true</c> (<c>Directory.Build.props</c>, or
/// <c>-p:FirebaseExperiments=true</c>); otherwise the package isn't even referenced and the skeleton's
/// <see cref="NoExperiments"/> stays (every answer is the app's own fallback), so the app can always call it.
/// </summary>
public static class FirebaseExperimentsSetup
{
    /// <summary>True when this build was made with <c>FirebaseExperiments=true</c>.</summary>
    public const bool IsBuiltIn =
#if FIREBASE_EXPERIMENTS
        true;
#else
        false;
#endif

    /// <param name="refreshEvery">
    /// How often the phone may ask the server for new variants (Firebase's default is 12 hours; zero while developing,
    /// so a change in the console shows on the next launch).
    /// </param>
    public static MauiAppBuilder AddFirebaseExperiments(this MauiAppBuilder builder, TimeSpan? refreshEvery = null)
    {
#if FIREBASE_EXPERIMENTS
        builder.ConfigureLifecycleEvents(events =>
        {
#if ANDROID
            events.AddAndroid(android => android.OnCreate((activity, _) => FirebaseCore.Start(activity)));
#elif IOS
            events.AddiOS(ios => ios.WillFinishLaunching((_, __) =>
            {
                FirebaseCore.Start();
                return false;
            }));
#endif
        });

        // Replace, not TryAdd: the skeleton registers NoExperiments as the default.
        builder.Services.Replace(ServiceDescriptor.Singleton<IExperiments>(services => new FirebaseRemoteExperiments(
            refreshEvery ?? FirebaseRemoteExperiments.DefaultRefreshEvery,
            services.GetRequiredService<Microsoft.Extensions.Logging.ILogger<FirebaseRemoteExperiments>>())));
#endif
        return builder;
    }
}
