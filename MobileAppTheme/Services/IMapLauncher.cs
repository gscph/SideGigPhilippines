namespace MobileAppTheme.Services;

/// <summary>
/// Opens a place in the phone's own map app: Google Maps or Waze, whichever is installed, else Google Maps in the
/// browser (<see cref="MapLinks"/>). Returns false if nothing could open it.
/// </summary>
public interface IMapLauncher
{
    /// <param name="place">Where to go.</param>
    /// <param name="label">The name the map app shows on the pin, where it can.</param>
    Task<bool> OpenAsync(GeoPoint place, string label);
}
