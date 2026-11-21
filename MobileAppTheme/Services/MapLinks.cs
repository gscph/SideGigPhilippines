using System.Globalization;

namespace MobileAppTheme.Services;

/// <summary>
/// Links for a place. <see cref="IMapLauncher"/> tries them in order: on Android a <see cref="Geo"/> link, which every
/// installed map app answers (Google Maps, Waze; the phone asks which one when there are several); on iOS the
/// <see cref="Waze"/> app, then the <see cref="GoogleMapsApp"/>; and when none is installed, Google Maps on the web
/// (<see cref="Web"/>) in the browser. <see cref="Frame"/> is a small OpenStreetMap view with a pin (free, no key) to
/// show inside a page (<c>MapFrame</c>).
/// </summary>
public static class MapLinks
{
    /// <summary>How far around the pin the small map shows, in degrees (about 450 m each way).</summary>
    public const double FrameSpanDegrees = 0.004;

    /// <summary>Android's map link with a named pin: <c>geo:0,0?q=lat,lng(label)</c>.</summary>
    public static string Geo(GeoPoint at, string label) =>
        $"geo:0,0?q={Coordinates(at)}({Uri.EscapeDataString(label)})";

    /// <summary>The Waze app, ready to drive there.</summary>
    public static string Waze(GeoPoint to) => $"waze://?ll={Coordinates(to)}&navigate=yes";

    /// <summary>The Google Maps app on iOS, with driving directions.</summary>
    public static string GoogleMapsApp(GeoPoint to) => $"comgooglemaps://?daddr={Coordinates(to)}&directionsmode=driving";

    /// <summary>Google Maps' directions page: the app takes it when installed, else the browser opens it.</summary>
    public static string Web(GeoPoint to) => $"https://www.google.com/maps/dir/?api=1&destination={Coordinates(to)}&travelmode=driving";

    /// <summary>OpenStreetMap's embeddable map around <paramref name="at"/>, with a pin on it.</summary>
    public static string Frame(GeoPoint at)
    {
        var box = string.Join(',',
            Number(at.Longitude - FrameSpanDegrees), Number(at.Latitude - FrameSpanDegrees),
            Number(at.Longitude + FrameSpanDegrees), Number(at.Latitude + FrameSpanDegrees));
        return $"https://www.openstreetmap.org/export/embed.html?bbox={box}&layer=mapnik&marker={Number(at.Latitude)},{Number(at.Longitude)}";
    }

    private static string Coordinates(GeoPoint point) => $"{Number(point.Latitude)},{Number(point.Longitude)}";

    private static string Number(double degrees) => Math.Round(degrees, 6).ToString(CultureInfo.InvariantCulture);
}
