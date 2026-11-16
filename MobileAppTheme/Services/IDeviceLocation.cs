namespace MobileAppTheme.Services;

/// <summary>
/// The phone's own location, asked only while the app is open ("while using the app", never in the background) and
/// only when the user taps a button that says why.
/// </summary>
public interface IDeviceLocation
{
    /// <summary>
    /// Asks for the location permission when the app doesn't have it yet, then reads where the phone is, as precisely
    /// as the phone allows. Never throws: every failure comes back as a <see cref="LocationFixStatus"/>.
    /// </summary>
    Task<LocationFix> GetCurrentAsync();

    /// <summary>Opens this app's page in the phone's Settings, where a denied permission can be turned on.</summary>
    void OpenAppSettings();
}

/// <summary>A point on the map in WGS 84 degrees.</summary>
public readonly record struct GeoPoint(double Latitude, double Longitude);

/// <summary>What a <see cref="IDeviceLocation.GetCurrentAsync"/> call ended with.</summary>
public enum LocationFixStatus
{
    /// <summary>The phone said where it is (<see cref="LocationFix.Point"/>, within <see cref="LocationFix.AccuracyMeters"/>).</summary>
    Found,

    /// <summary>The user said no to location, now or before (it can be turned on in Settings).</summary>
    Denied,

    /// <summary>Location is switched off on the phone.</summary>
    Off,

    /// <summary>The phone couldn't tell in time (indoors, no signal) or the read failed; the adapter logged why.</summary>
    Failed,
}

/// <summary>
/// Where the phone is. <see cref="AccuracyMeters"/> is the phone's own estimate (null when it gave none); a phone with
/// only "approximate location" allowed answers within a few kilometres.
/// </summary>
public sealed record LocationFix(LocationFixStatus Status, GeoPoint? Point = null, double? AccuracyMeters = null)
{
    public static readonly LocationFix Denied = new(LocationFixStatus.Denied);
    public static readonly LocationFix Off = new(LocationFixStatus.Off);
    public static readonly LocationFix Failed = new(LocationFixStatus.Failed);
}
