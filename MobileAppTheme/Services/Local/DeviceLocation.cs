using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;

namespace MobileAppTheme.Services.Local;

/// <summary>
/// The phone's location via MAUI <see cref="Geolocation"/>, with the "while using the app" permission
/// (<see cref="Permissions.LocationWhenInUse"/>; Android needs ACCESS_FINE_LOCATION and ACCESS_COARSE_LOCATION in the
/// manifest, iOS NSLocationWhenInUseUsageDescription). Asks for the best accuracy; a phone where the user allowed only
/// approximate location answers anyway, with a large <see cref="LocationFix.AccuracyMeters"/>.
/// </summary>
public sealed class DeviceLocation(ILogger<DeviceLocation> logger) : IDeviceLocation
{
    /// <summary>How long to wait for the phone to find itself before giving up.</summary>
    public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);

    public async Task<LocationFix> GetCurrentAsync()
    {
        try
        {
            var status = await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                var current = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                return current == PermissionStatus.Granted ? current : await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            });
            if (status != PermissionStatus.Granted)
            {
                logger.LogInformation("Location permission not given ({Status}).", status);
                return LocationFix.Denied;
            }

            var request = new GeolocationRequest(GeolocationAccuracy.Best, Timeout);
            var location = await MainThread.InvokeOnMainThreadAsync(() => Geolocation.Default.GetLocationAsync(request));
            if (location is null)
            {
                logger.LogWarning("The phone gave no location within {Timeout}.", Timeout);
                return LocationFix.Failed;
            }

            return new LocationFix(LocationFixStatus.Found, new GeoPoint(location.Latitude, location.Longitude), location.Accuracy);
        }
        catch (FeatureNotEnabledException ex)
        {
            logger.LogInformation(ex, "Location is off on the phone.");
            return LocationFix.Off;
        }
        catch (PermissionException ex)
        {
            logger.LogInformation(ex, "Location permission denied.");
            return LocationFix.Denied;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Couldn't read the phone's location.");
            return LocationFix.Failed;
        }
    }

    public void OpenAppSettings() => AppInfo.Current.ShowSettingsUI();
}
