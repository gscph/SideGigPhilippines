using Microsoft.Maui.Networking;

namespace MobileAppTheme.Services.Local;

/// <summary>
/// <see cref="INetworkStatus"/> from MAUI's <see cref="Connectivity"/> (Android needs <c>ACCESS_NETWORK_STATE</c>) and the
/// last request's fate: offline when the phone has no network, or when the last request got no answer
/// (<see cref="NoAnswer"/>) and none has been answered since. A change of network (Wi-Fi back, data on) clears that, so
/// the next request gets its chance. Where the platform can't tell (tests, an unsupported platform) the phone counts as
/// connected, so nothing is hidden by mistake.
/// </summary>
public sealed class DeviceNetworkStatus : INetworkStatus
{
    private bool _listening;
    private Action? _changed;

    // The last request got no answer and none has since.
    private volatile bool _unanswered;

    public bool IsOnline => !_unanswered && DeviceConnected;

    public event Action? Changed
    {
        add
        {
            _changed += value;
            Listen();
        }
        remove => _changed -= value;
    }

    public void NoAnswer() => SetUnanswered(true);

    public void Answered() => SetUnanswered(false);

    private void SetUnanswered(bool unanswered)
    {
        if (_unanswered == unanswered)
        {
            return;
        }

        _unanswered = unanswered;
        _changed?.Invoke();
    }

    private static bool DeviceConnected
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

    private void Listen()
    {
        if (_listening)
        {
            return;
        }

        try
        {
            Connectivity.Current.ConnectivityChanged += (_, _) =>
            {
                // Another network: the requests that failed on the old one say nothing about it.
                _unanswered = false;
                _changed?.Invoke();
            };
            _listening = true;
        }
        catch (Exception)
        {
            // No connectivity service here: only the requests' answers count.
        }
    }
}
