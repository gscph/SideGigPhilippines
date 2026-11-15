#if FIREBASE_EXPERIMENTS
using Microsoft.Extensions.Logging;
using MobileAppTheme.Services;
using Plugin.Firebase.RemoteConfig;

namespace MobileAppTheme.Firebase;

/// <summary>
/// Firebase Remote Config (the values Firebase A/B Testing hands out) behind the skeleton's <see cref="IExperiments"/>
/// (Plugin.Firebase.RemoteConfig 3.1.x). Registered by <see cref="FirebaseExperimentsSetup.AddFirebaseExperiments"/>,
/// compiled only with <c>FirebaseExperiments=true</c>. A key the console doesn't have, a failed fetch or a Firebase that
/// couldn't start (no google-services.json) all answer the app's fallback.
/// </summary>
public sealed class FirebaseRemoteExperiments(TimeSpan refreshEvery, ILogger<FirebaseRemoteExperiments> logger) : IExperiments
{
    public static readonly TimeSpan DefaultRefreshEvery = TimeSpan.FromHours(12);

    /// <summary>The longest a fetch may take; past it the last known variants stay.</summary>
    private static readonly TimeSpan FetchTimeout = TimeSpan.FromSeconds(15);

    public bool IsOn => FirebaseCore.IsRunning;

    public async Task RefreshAsync()
    {
        if (!IsOn)
        {
            return;
        }

        try
        {
            var config = CrossFirebaseRemoteConfig.Current;
            await config.EnsureInitializedAsync();
            await config.SetRemoteConfigSettingsAsync(new RemoteConfigSettings(refreshEvery, FetchTimeout));
            await config.FetchAndActivateAsync();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Firebase Remote Config couldn't refresh; the last known variants stay.");
        }
    }

    public string Variant(string key, string fallback) => Read(key) ?? fallback;

    public bool IsEnabled(string key, bool fallback) => bool.TryParse(Read(key), out var enabled) ? enabled : fallback;

    /// <summary>The server's value for the key, or null when there is none (Remote Config answers "" for an unknown key).</summary>
    private string? Read(string key)
    {
        if (!IsOn)
        {
            return null;
        }

        try
        {
            var value = CrossFirebaseRemoteConfig.Current.GetString(key);
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Firebase Remote Config couldn't read {Key}.", key);
            return null;
        }
    }
}
#endif
