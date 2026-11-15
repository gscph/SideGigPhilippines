#if FIREBASE_ANALYTICS
using Microsoft.Extensions.Logging;
using MobileAppTheme.Services;
using Plugin.Firebase.Analytics;

namespace MobileAppTheme.Firebase;

/// <summary>
/// Google Analytics for Firebase behind the skeleton's <see cref="IAnalytics"/> (Plugin.Firebase.Analytics 3.1.x).
/// Registered by <see cref="FirebaseAnalyticsSetup.AddFirebaseAnalytics"/>, compiled only with
/// <c>FirebaseAnalytics=true</c>. Names and values are cut to Firebase's limits; a failure is logged and the event is
/// dropped, never a crash. When Firebase couldn't start (no google-services.json) it records nothing.
/// </summary>
public sealed class FirebaseUsageAnalytics(ILogger<FirebaseUsageAnalytics> logger) : IAnalytics
{
    // Firebase's limits: longer names or values make it drop the event or the property.
    private const int MaxName = 40;
    private const int MaxValue = 100;
    private const int MaxPropertyName = 24;
    private const int MaxPropertyValue = 36;

    public bool IsOn => FirebaseCore.IsRunning;

    public void Screen(string name) =>
        Event("screen_view", new Dictionary<string, string> { ["screen_name"] = name, ["screen_class"] = name });

    public void Event(string name, IReadOnlyDictionary<string, string>? details = null) =>
        Try("log an event", analytics => analytics.LogEvent(
            Cut(name, MaxName),
            (details ?? new Dictionary<string, string>()).ToDictionary(d => Cut(d.Key, MaxName), d => (object)Cut(d.Value, MaxValue))));

    public void SetProperty(string name, string? value) =>
        Try("set a property", analytics => analytics.SetUserProperty(Cut(name, MaxPropertyName), value is null ? null! : Cut(value, MaxPropertyValue)));

    public void SetEnabled(bool enabled) =>
        Try("turn collection on or off", analytics => analytics.IsAnalyticsCollectionEnabled = enabled);

    private void Try(string what, Action<IFirebaseAnalytics> act)
    {
        if (!IsOn)
        {
            return;
        }

        try
        {
            act(CrossFirebaseAnalytics.Current);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Firebase Analytics couldn't {What}.", what);
        }
    }

    private static string Cut(string text, int max) => text.Length > max ? text[..max] : text;
}
#endif
