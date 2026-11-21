using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services;

/// <summary>Which way a place lies from here, on an eight-point compass.</summary>
public enum CompassPoint
{
    North,
    NorthEast,
    East,
    SouthEast,
    South,
    SouthWest,
    West,
    NorthWest,
}

/// <summary>
/// The household's exact place for the worker it picked (<see cref="PickedJob.MeetPoint"/>): who may send or see it,
/// and how far and which way it is from the worker, in a straight line (Waze or Google Maps gives the road). The
/// household sends it only on a tap, from the phone's precise location (<see cref="WorkArea.Read"/>: never a rough
/// point); only the picked worker sees it, and only until the job is called off or done.
/// </summary>
public static class MeetLocation
{
    private const double EarthRadiusKm = 6371.0;

    /// <summary>The household may send (or send again) its place: a picked job not yet done.</summary>
    public static bool CanSend(PickedJob job) => job.IAmTheHousehold && !job.IsCompleted;

    /// <summary>The worker has a place to go to.</summary>
    public static bool CanSee(PickedJob job) => !job.IAmTheHousehold && !job.IsCompleted && job.MeetPoint is not null;

    /// <summary>The worker still waits for the household's place.</summary>
    public static bool IsWaiting(PickedJob job) => !job.IAmTheHousehold && !job.IsCompleted && job.MeetPoint is null;

    /// <summary>The straight-line distance between two points (haversine).</summary>
    public static double DistanceKm(GeoPoint from, GeoPoint to)
    {
        var dLat = Radians(to.Latitude - from.Latitude);
        var dLon = Radians(to.Longitude - from.Longitude);
        var a = Math.Pow(Math.Sin(dLat / 2), 2)
                + Math.Cos(Radians(from.Latitude)) * Math.Cos(Radians(to.Latitude)) * Math.Pow(Math.Sin(dLon / 2), 2);
        return 2 * EarthRadiusKm * Math.Asin(Math.Min(1, Math.Sqrt(a)));
    }

    /// <summary>The compass bearing from one point to the other, 0–360° clockwise from north.</summary>
    public static double BearingDegrees(GeoPoint from, GeoPoint to)
    {
        var lat1 = Radians(from.Latitude);
        var lat2 = Radians(to.Latitude);
        var dLon = Radians(to.Longitude - from.Longitude);
        var y = Math.Sin(dLon) * Math.Cos(lat2);
        var x = Math.Cos(lat1) * Math.Sin(lat2) - Math.Sin(lat1) * Math.Cos(lat2) * Math.Cos(dLon);
        return (Math.Atan2(y, x) * 180 / Math.PI + 360) % 360;
    }

    /// <summary>Which way <paramref name="to"/> lies from <paramref name="from"/>.</summary>
    public static CompassPoint Direction(GeoPoint from, GeoPoint to) =>
        (CompassPoint)((int)Math.Round(BearingDegrees(from, to) / 45.0) % 8);

    /// <summary>A distance as people say it: metres to the nearest 50 under 1 km, one decimal under 10 km, else whole km.</summary>
    public static (double Amount, bool Metres) Rounded(double km) =>
        km < 1 ? (Math.Max(50, Math.Round(km * 1000 / 50) * 50), true)
        : km < 10 ? (Math.Round(km, 1), false)
        : (Math.Round(km), false);

    private static double Radians(double degrees) => degrees * Math.PI / 180;
}
