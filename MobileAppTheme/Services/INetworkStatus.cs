namespace MobileAppTheme.Services;

/// <summary>Whether the phone has internet now, and when that changes (<see cref="Local.DeviceNetworkStatus"/>).</summary>
public interface INetworkStatus
{
    bool IsOnline { get; }

    /// <summary>Raised on any thread when <see cref="IsOnline"/> may have changed.</summary>
    event Action? Changed;
}
