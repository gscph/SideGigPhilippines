using System.Reflection;
using SideGigPh.Contracts;

namespace SideGigPh.Maui.Services.Diagnostics;

/// <summary>
/// Which build this is, stamped into the app when it was built (<c>Directory.Build.targets</c>' <c>StampBuildInfo</c>):
/// the version from <c>Directory.Build.props</c> plus the CI build number, the git commit and the channel. Shown small
/// on the splash and the top bar, sent with every error report and on every API call (<see cref="ClientHeader"/>).
/// </summary>
/// <param name="Version">The version label, e.g. <c>0.1.0-rc.12</c> (<c>0.1.0-rc</c> when built on a PC).</param>
/// <param name="CodeVersion">The git commit, e.g. <c>48466c9</c> (<c>-dirty</c> with uncommitted changes).</param>
/// <param name="Channel">One of <see cref="AppChannels"/>.</param>
public sealed record BuildInfo(string Version, string CodeVersion, string Channel)
{
    public const string Unknown = "unknown";

    /// <summary>This app's build.</summary>
    public static BuildInfo Current { get; } = From(typeof(BuildInfo).Assembly);

    /// <summary>What people see: <c>v0.1.0-rc.12</c>.</summary>
    public string Label => $"v{Version}";

    /// <summary>The value of the <see cref="ApiHeaders.AppVersion"/> header: <c>0.1.0-rc.12 (beta; 48466c9)</c>.</summary>
    public string ClientHeader => $"{Version} ({Channel}; {CodeVersion})";

    /// <summary>The version an error report carries: the label and the store build number, e.g. <c>0.1.0-rc.12 (12)</c>.</summary>
    public string WithStoreBuild(string storeBuild) => $"{Version} ({storeBuild})";

    /// <summary>The build stamped into <paramref name="assembly"/>; "unknown" and <c>dev</c> for what isn't there.</summary>
    public static BuildInfo From(Assembly assembly)
    {
        var stamped = assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .Where(a => !string.IsNullOrWhiteSpace(a.Value))
            .GroupBy(a => a.Key)
            .ToDictionary(g => g.Key, g => g.First().Value!);
        return new BuildInfo(
            stamped.GetValueOrDefault("AppVersion", Unknown),
            stamped.GetValueOrDefault("CodeVersion", Unknown),
            stamped.GetValueOrDefault("AppChannel") is { } channel && AppChannels.IsKnown(channel) ? channel : AppChannels.Dev);
    }
}
