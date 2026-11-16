namespace SideGigPh.Maui.Services;

/// <summary>What the app makes of a location the phone gave (<see cref="WorkArea.Read"/>).</summary>
public enum LocationOutcome
{
    /// <summary>A precise point in the Philippines: use it.</summary>
    Set,

    /// <summary>The person said no to location (it can be turned on in Settings).</summary>
    Denied,

    /// <summary>Location is off on the phone.</summary>
    Off,

    /// <summary>Only a rough point (the phone allows "approximate location"): not used, the town's centre stays.</summary>
    Rough,

    /// <summary>A point outside the Philippines (a GPS glitch, or abroad): the server would refuse it.</summary>
    Abroad,

    /// <summary>The phone couldn't tell.</summary>
    Failed,
}

/// <summary>
/// The rules for a precise point (a worker's base, a post's place) and a worker's travel distance. Without a point the
/// server uses the centre of the towns, so nothing ever waits on location. The point never leaves the server
/// (others see a town or whole kilometres).
/// </summary>
public static class WorkArea
{
    /// <summary>The server's <c>GeoRules</c>: 1–50 km, 10 until the worker picks.</summary>
    public const int MinRadiusKm = 1;
    public const int MaxRadiusKm = 50;
    public const int DefaultRadiusKm = 10;

    /// <summary>The distances a worker picks from.</summary>
    public static readonly IReadOnlyList<int> RadiusChoices = [3, 5, 10, 20, 30, 50];

    /// <summary>A point the phone is less sure of than this is "rough": not used.</summary>
    public const double RoughAfterMeters = 1000;

    public static LocationOutcome Read(LocationFix fix) => fix.Status switch
    {
        LocationFixStatus.Denied => LocationOutcome.Denied,
        LocationFixStatus.Off => LocationOutcome.Off,
        LocationFixStatus.Found when fix.Point is not { } point => LocationOutcome.Failed,
        LocationFixStatus.Found when !InPhilippines(fix.Point.Value) => LocationOutcome.Abroad,
        LocationFixStatus.Found when fix.AccuracyMeters > RoughAfterMeters => LocationOutcome.Rough,
        LocationFixStatus.Found => LocationOutcome.Set,
        _ => LocationOutcome.Failed,
    };

    /// <summary>The server's loose box around the Philippines (<c>GeoRules.Point</c>).</summary>
    public static bool InPhilippines(GeoPoint point) =>
        point.Latitude is >= 4.0 and <= 21.5 && point.Longitude is >= 116.0 and <= 127.0;

    public static int ClampRadius(int radiusKm) => Math.Clamp(radiusKm, MinRadiusKm, MaxRadiusKm);
}
