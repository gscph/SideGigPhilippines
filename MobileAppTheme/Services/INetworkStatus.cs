namespace MobileAppTheme.Services;

/// <summary>
/// Whether the phone has internet now, and when that changes (<see cref="Local.DeviceNetworkStatus"/>). The phone's own
/// view (a network is connected) is not enough: with broken DNS or a captive Wi-Fi it says "online" while nothing gets
/// through. So the app's web client also reports each request: one that got no answer (<see cref="NetworkErrors.IsNetwork"/>)
/// counts the phone as offline until a request gets an answer again.
/// </summary>
public interface INetworkStatus
{
    bool IsOnline { get; }

    /// <summary>Raised on any thread when <see cref="IsOnline"/> may have changed.</summary>
    event Action? Changed;

    /// <summary>
    /// A web request couldn't reach the server (no signal, no DNS, dropped socket): the phone is offline until one gets
    /// an answer. A request that only ran out of time is not reported: a slow server isn't a lost connection.
    /// </summary>
    void NoAnswer();

    /// <summary>A web request got an answer (any status, even an error): the connection works.</summary>
    void Answered();
}
