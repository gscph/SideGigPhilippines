using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Maui.LifecycleEvents;
using MobileAppTheme.Services;

namespace MobileAppTheme.Firebase;

/// <summary>
/// One call in <c>MauiProgram</c> turns on Google Analytics for Firebase: <c>builder.AddFirebaseAnalytics()</c>,
/// behind the skeleton's <see cref="IAnalytics"/>. It is a build-time feature flag: the call only does something in
/// a build made with <c>FirebaseAnalytics=true</c> (<c>Directory.Build.props</c>, or <c>-p:FirebaseAnalytics=true</c>);
/// otherwise the package isn't even referenced and the skeleton's <see cref="NoAnalytics"/> stays, so the app can
/// always call it.
/// </summary>
public static class FirebaseAnalyticsSetup
{
    /// <summary>True when this build was made with <c>FirebaseAnalytics=true</c>.</summary>
    public const bool IsBuiltIn =
#if FIREBASE_ANALYTICS
        true;
#else
        false;
#endif

    public static MauiAppBuilder AddFirebaseAnalytics(this MauiAppBuilder builder)
    {
#if FIREBASE_ANALYTICS
        builder.ConfigureLifecycleEvents(events =>
        {
#if ANDROID
            events.AddAndroid(android => android.OnCreate((activity, _) =>
            {
                FirebaseCore.Start(activity);
                FirebaseCore.TryRun("Analytics", () => Plugin.Firebase.Analytics.FirebaseAnalyticsImplementation.Initialize(activity));
            }));
#elif IOS
            events.AddiOS(ios => ios.WillFinishLaunching((_, __) =>
            {
                FirebaseCore.Start();
                return false;
            }));
#endif
        });

        // Replace, not TryAdd: the skeleton registers NoAnalytics as the default.
        builder.Services.Replace(ServiceDescriptor.Singleton<IAnalytics, FirebaseUsageAnalytics>());
#endif
        return builder;
    }
}
