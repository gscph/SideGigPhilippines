using Microsoft.Maui.Networking;

namespace MobileAppTheme.Services.Local;

/// <summary>
/// <see cref="INetworkStatus"/> from MAUI's <see cref="Connectivity"/> (Android needs <c>ACCESS_NETWORK_STATE</c>).
/// Where the platform can't tell (tests, an unsupported platform) it says online, so nothing is hidden by mistake.
/// </summary>
public sealed class DeviceNetworkStatus : INetworkStatus
{
    private bool _listening;
    private Action? _changed;

    public bool IsOnline
    {
        get
        {
            try
            {
                return Connectivity.Current.NetworkAccess is not (NetworkAccess.None or NetworkAccess.Local);
            }
            catch (Exception)
            {
                return true;
            }
        }
    }

    public event Action? Changed
    {
        add
        {
            _changed += value;
            Listen();
        }
        remove => _changed -= value;
    }

    private void Listen()
    {
        if (_listening)
        {
            return;
        }

        try
        {
            Connectivity.Current.ConnectivityChanged += (_, _) => _changed?.Invoke();
            _listening = true;
        }
        catch (Exception)
        {
            // No connectivity service here: IsOnline stays true.
        }
    }
}
