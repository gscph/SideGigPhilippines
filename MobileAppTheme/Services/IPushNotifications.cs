namespace MobileAppTheme.Services;

/// <summary>
/// Notifications from the app's server to this phone (push), and the app icon's badge. A provider (Firebase Cloud
/// Messaging, Azure Notification Hubs…) is an adapter in its own project that replaces <see cref="NoPushNotifications"/>.
/// Data in a notification is plain text keys and values the app and its server agree on.
/// </summary>
public interface IPushNotifications
{
    /// <summary><c>"android"</c> or <c>"ios"</c>; null when this app can't get notifications (no provider).</summary>
    string? Platform { get; }

    /// <summary>True when the person allows this app's notifications (never asks).</summary>
    Task<bool> IsAllowedAsync();

    /// <summary>Asks the person to allow notifications (the system dialog, which the system shows at most once or twice). True when allowed.</summary>
    Task<bool> RequestPermissionAsync();

    /// <summary>The address the server sends this phone's notifications to, or null when there isn't one yet.</summary>
    Task<string?> GetTokenAsync();

    /// <summary>The address changed; the server needs the new one.</summary>
    event Action<string>? TokenChanged;

    /// <summary>A notification arrived while the app was open (none is shown; the app updates itself).</summary>
    event Action<IReadOnlyDictionary<string, string>>? Received;

    /// <summary>The person tapped a notification. A tap that opened the app before anyone listened goes to the first listener.</summary>
    event Action<IReadOnlyDictionary<string, string>>? Tapped;

    /// <summary>
    /// Keeps the shown notifications whose tag is in <paramref name="unreadTags"/>, clears the others, and sets the app
    /// icon's badge to how many there are. Call it whenever what's unread changes.
    /// </summary>
    void ShowOnly(IReadOnlyCollection<string> unreadTags);
}

/// <summary>The default: no notifications and no badge, until the app adds a provider (e.g. <c>AddFirebasePush</c>).</summary>
public sealed class NoPushNotifications : IPushNotifications
{
    public string? Platform => null;

    public Task<bool> IsAllowedAsync() => Task.FromResult(false);

    public Task<bool> RequestPermissionAsync() => Task.FromResult(false);

    public Task<string?> GetTokenAsync() => Task.FromResult<string?>(null);

    public event Action<string>? TokenChanged { add { } remove { } }

    public event Action<IReadOnlyDictionary<string, string>>? Received { add { } remove { } }

    public event Action<IReadOnlyDictionary<string, string>>? Tapped { add { } remove { } }

    public void ShowOnly(IReadOnlyCollection<string> unreadTags)
    {
    }
}

/// <summary>An Android notification channel the app creates at launch (iOS has no channels). Quiet = no sound or pop-up, only the tray line and icon dot.</summary>
public sealed record PushChannel(string Id, string Name, bool Alert);
