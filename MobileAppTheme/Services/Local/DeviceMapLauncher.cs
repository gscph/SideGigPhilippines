using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices;

namespace MobileAppTheme.Services.Local;

/// <summary>
/// Opens a place with MAUI <see cref="Launcher"/>, trying the installed map apps first (<see cref="MapLinks"/>): on
/// Android a <c>geo:</c> link (Google Maps, Waze, any map app; needs the <c>geo</c> VIEW entry in the manifest's
/// &lt;queries&gt;), on iOS <c>waze://</c> then <c>comgooglemaps://</c> (both in <c>LSApplicationQueriesSchemes</c>), then
/// Google Maps on the web in the browser.
/// </summary>
public sealed class DeviceMapLauncher(ILogger<DeviceMapLauncher> logger) : IMapLauncher
{
    public async Task<bool> OpenAsync(GeoPoint place, string label)
    {
        try
        {
            return await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                foreach (var app in AppLinks(place, label))
                {
                    if (await Launcher.Default.TryOpenAsync(new Uri(app)))
                    {
                        return true;
                    }
                }

                return await Launcher.Default.OpenAsync(new Uri(MapLinks.Web(place)));
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not open a map app.");
            return false;
        }
    }

    private static string[] AppLinks(GeoPoint place, string label) =>
        DeviceInfo.Current.Platform == DevicePlatform.iOS
            ? [MapLinks.Waze(place), MapLinks.GoogleMapsApp(place)]
            : [MapLinks.Geo(place, label)];
}
