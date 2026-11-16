namespace SideGigPh.AzureSQL;

/// <summary>A point on the map (WGS 84 degrees).</summary>
public readonly record struct GeoPoint(double Latitude, double Longitude);

/// <summary>
/// The rules for where a worker is based and how far they travel (<c>013-schema-geo.sql</c>). A worker's point is
/// private: other people only ever get a town or a rounded distance.
/// </summary>
public static class GeoRules
{
    public const short MinRadiusKm = 1;
    public const short MaxRadiusKm = 50;

    /// <summary>The travel distance a listing starts with until the worker picks one.</summary>
    public const short DefaultRadiusKm = 10;

    /// <summary>
    /// The centre of each town we serve (town ids as in <c>ConfigOptions</c>), used until the worker gives their own
    /// point. The same three are in the backfill of <c>013-schema-geo.sql</c>: a new town needs a line here.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, GeoPoint> TownCentres = new Dictionary<string, GeoPoint>
    {
        ["angono"] = new(14.5266, 121.1536),
        ["antipolo"] = new(14.5865, 121.1763),
        ["taytay"] = new(14.5574, 121.1324),
    };

    /// <summary>
    /// A point a phone may send: inside the Philippines (a loose box, so a GPS glitch at 0,0 or a faked point abroad
    /// is refused), or null when either number is missing or out of range.
    /// </summary>
    public static GeoPoint? Point(double? latitude, double? longitude) =>
        latitude is >= 4.0 and <= 21.5 && longitude is >= 116.0 and <= 127.0 ? new GeoPoint(latitude.Value, longitude.Value) : null;

    /// <summary>The middle of the centres of the towns we know among <paramref name="cityIds"/>, or null when we know none.</summary>
    public static GeoPoint? CentreOf(IEnumerable<string> cityIds)
    {
        var centres = cityIds.Distinct().Where(TownCentres.ContainsKey).Select(id => TownCentres[id]).ToList();
        return centres.Count == 0 ? null : new GeoPoint(centres.Average(c => c.Latitude), centres.Average(c => c.Longitude));
    }

    public static bool IsRadius(int? radiusKm) => radiusKm is >= MinRadiusKm and <= MaxRadiusKm;
}
