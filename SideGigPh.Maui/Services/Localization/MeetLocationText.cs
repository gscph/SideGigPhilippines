using System.Globalization;

namespace SideGigPh.Maui.Services.Localization;

/// <summary>The words for the household's exact place on the worker's card (<see cref="MeetLocation"/>).</summary>
public static class MeetLocationText
{
    /// <summary>"About 4.2 km to the northeast of you, in a straight line."</summary>
    public static string MeetDistance(this AppText text, GeoPoint from, GeoPoint to)
    {
        var (amount, metres) = MeetLocation.Rounded(MeetLocation.DistanceKm(from, to));
        var distance = text.Format(metres ? Copy.MeetMetres : Copy.MeetKm, amount.ToString("0.#", CultureInfo.InvariantCulture));
        return text.Format(Copy.MeetDistance, distance, text.Get(Compass(MeetLocation.Direction(from, to))));
    }

    private static LocalizedText Compass(CompassPoint point) => point switch
    {
        CompassPoint.North => Copy.CompassNorth,
        CompassPoint.NorthEast => Copy.CompassNorthEast,
        CompassPoint.East => Copy.CompassEast,
        CompassPoint.SouthEast => Copy.CompassSouthEast,
        CompassPoint.South => Copy.CompassSouth,
        CompassPoint.SouthWest => Copy.CompassSouthWest,
        CompassPoint.West => Copy.CompassWest,
        _ => Copy.CompassNorthWest,
    };
}
