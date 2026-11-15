using System.Reflection;

namespace SideGigPh.Api;

/// <summary>
/// Which build of the API this is, stamped in when it was built (<c>Directory.Build.targets</c>' <c>StampBuildInfo</c>):
/// the version from <c>Directory.Build.props</c> with the deploy's build number, and the git commit. Sent on every
/// answer (<see cref="Contracts.ApiHeaders.ApiVersion"/>), so the app's error reports name the API build that answered.
/// </summary>
internal static class ApiVersion
{
    /// <summary>E.g. <c>0.1.0-rc.57+48466c9</c>; <c>unknown</c> parts when built without the stamp.</summary>
    public static string Current { get; } = From(typeof(ApiVersion).Assembly);

    public static string From(Assembly assembly)
    {
        var stamped = assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .Where(a => !string.IsNullOrWhiteSpace(a.Value))
            .GroupBy(a => a.Key)
            .ToDictionary(g => g.Key, g => g.First().Value!);
        return $"{stamped.GetValueOrDefault("AppVersion", "unknown")}+{stamped.GetValueOrDefault("CodeVersion", "unknown")}";
    }
}
