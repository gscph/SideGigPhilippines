using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services.Diagnostics;

namespace SideGigPh.Maui.Services;

/// <summary>
/// Loads the app's config (<see cref="AppConfig"/>: trades and jobs, towns, price tiers, experience, urgency, goals)
/// <b>once</b>, while the launch splash shows (<c>LaunchRouter</c>), and hands it to <see cref="ReferenceData"/>, which
/// every page reads. Pages never fetch it. The skeleton's <see cref="RemoteConfig{T}"/> decides where it comes from:
/// the phone's copy while it is under 12 hours old and saved by this build, else the server (saved for next time), else the old copy, else the
/// copy built into the app.
/// </summary>
public sealed class AppConfigService
{
    private readonly RemoteConfig<AppConfig> _remote;
    private readonly Action<AppConfig> _use;
    private Task? _loading;

    public AppConfigService(RemoteConfig<AppConfig> remote)
        : this(remote, ReferenceData.UseConfig)
    {
    }

    /// <summary>For tests: <paramref name="use"/> receives the config instead of <see cref="ReferenceData.UseConfig"/>.</summary>
    internal AppConfigService(RemoteConfig<AppConfig> remote, Action<AppConfig> use)
    {
        _remote = remote;
        _use = use;
    }

    /// <summary>
    /// The rules <see cref="RemoteConfig{T}"/> uses for the app's config. A copy saved by another build (an update, a
    /// reinstall) is checked with the server at the first launch, so a new version never runs on the old trade list.
    /// </summary>
    public static RemoteConfigRules<AppConfig> Rules { get; } = new(ReferenceData.CanUse)
    {
        AppBuild = $"{BuildInfo.Current.Version} {BuildInfo.Current.CodeVersion}",
    };

    /// <summary>Loads and applies the config. Runs once per app run; later calls return at once.</summary>
    public Task LoadAsync() => _loading ??= LoadOnceAsync();

    private async Task LoadOnceAsync()
    {
        if (await _remote.LoadAsync() is { } config)
        {
            _use(config);
        }
    }
}
