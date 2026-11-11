using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel.Communication;

namespace MobileAppTheme.Services.Local;

/// <summary>
/// Opens the phone's dialer or SMS app via MAUI Essentials. Android 11+ needs the matching
/// &lt;queries&gt; entries in AndroidManifest.xml, or both report "not supported".
/// </summary>
public sealed class DeviceContactLauncher(ILogger<DeviceContactLauncher> logger) : IContactLauncher
{
    public Task<bool> CallAsync(string phoneNumber)
    {
        try
        {
            PhoneDialer.Default.Open(phoneNumber);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not open the dialer.");
            return Task.FromResult(false);
        }
    }

    public async Task<bool> TextAsync(string phoneNumber, string message)
    {
        try
        {
            await Sms.Default.ComposeAsync(new SmsMessage(message, phoneNumber));
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not open the SMS composer.");
            return false;
        }
    }
}
