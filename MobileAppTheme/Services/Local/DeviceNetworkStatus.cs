using Microsoft.Maui.Networking;

namespace MobileAppTheme.Services.Local;

/// <summary>
/// <see cref="INetworkStatus"/> from MAUI's <see cref="Connectivity"/> (Android needs <c>ACCESS_NETWORK_STATE</c>) and the
/// last request's fate: offline when the phone has no network, or when the last request got no answer
/// (<see cref="NoAnswer"/>) and none has been answered since. A change of network (Wi-Fi back, data on) clears that, so
/// the next request gets its chance. Where the platform can't tell (tests, an unsupported platform) the phone counts as
/// connected, so nothing is hidden by mistake.
/// <para>
/// The platform's word is not final either: iOS's reachability check sometimes says "no network" (or "local only")
/// while requests go through, e.g. after coming back from the background, on a VPN, or with a stale report it never
/// updates. So a request that got an answer counts the phone as online even when the platform says otherwise, until the
/// platform reports another change of network.
/// </para>
/// <para>
/// Switching networks (5G to Wi-Fi) breaks the requests already on their way, and their failures arrive just after the
/// platform reported the change. So for <see cref="SettleAfterChange"/> after a change a request with no answer doesn't
/// count: it went out on the old network.
/// </para>
/// </summary>
public sealed class DeviceNetworkStatus : INetworkStatus
{
    private bool _listening;
    private Action? _changed;

    // The last request got no answer and none has since.
    private volatile bool _unanswered;

    // A request got an answer since the platform last reported a change of network: the internet works, whatever the
    // platform says.
    private volatile bool _answered;

    // When the platform last reported a change of network (Environment.TickCount64), or long ago.
    private long _changedAt = long.MinValue / 2;

    /// <summary>How long after a change of network a request with no answer is put down to the switch, not counted.</summary>
    public static readonly TimeSpan SettleAfterChange = TimeSpan.FromSeconds(10);

    public bool IsOnline => !_unanswered && (_answered || DeviceConnected);

    public event Action? Changed
    {
        add
        {
            _changed += value;
            Listen();
        }
        remove => _changed -= value;
    }

    public void NoAnswer()
    {
        if (Environment.TickCount64 - Interlocked.Read(ref _changedAt) < (long)SettleAfterChange.TotalMilliseconds)
        {
            return;
        }

        var wasOnline = IsOnline;
        _unanswered = true;
        _answered = false;
        Announce(wasOnline);
    }

    public void Answered()
    {
        var wasOnline = IsOnline;
        _unanswered = false;
        _answered = true;
        Announce(wasOnline);
    }

    private void Announce(bool wasOnline)
    {
        if (IsOnline != wasOnline)
        {
            _changed?.Invoke();
        }
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
                // Another network: the requests on the old one, answered or not, say nothing about it.
                Interlocked.Exchange(ref _changedAt, Environment.TickCount64);
                _unanswered = false;
                _answered = false;
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
