#if ANDROID
using Plugin.Firebase.Core.Platforms.Android;
#elif IOS
using Plugin.Firebase.Core.Platforms.iOS;
#endif

namespace MobileAppTheme.Firebase;

/// <summary>
/// Starts the native Firebase SDK once, whichever setup call (<c>AddFirebaseAuth</c>, <c>AddFirebasePush</c>) runs
/// first. Initialization throws when google-services.json / GoogleService-Info.plist is missing; that must not crash
/// launch, so it's logged and the feature fails gracefully instead.
/// </summary>
internal static class FirebaseCore
{
    private static readonly object Gate = new();
    private static bool _started;

    /// <summary>True when the SDK is running (false when its config file is missing).</summary>
    public static bool IsRunning { get; private set; }

#if ANDROID
    public static void Start(Android.App.Activity activity) => StartOnce(() => CrossFirebase.Initialize(activity));
#elif IOS
    public static void Start() => StartOnce(() => CrossFirebase.Initialize());
#endif

    /// <summary>Runs a feature's own initialization, logging instead of crashing when it fails.</summary>
    public static void TryRun(string what, Action initialize)
    {
        if (!IsRunning)
        {
            return;
        }

        try
        {
            initialize();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Firebase] {what} skipped: {ex}");
        }
    }

    private static void StartOnce(Action initialize)
    {
        lock (Gate)
        {
            if (_started)
            {
                return;
            }

            _started = true;
            try
            {
                initialize();
                IsRunning = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Firebase] initialization skipped: {ex}");
            }
        }
    }
}
