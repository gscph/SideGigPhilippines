namespace MobileAppTheme.Services;

/// <summary>
/// Whether the app is on screen. The app's <c>Window</c> sets it from its <c>Stopped</c> / <c>Resumed</c> events;
/// background work (polling an open chat) skips its network calls while the app is hidden, so a phone left on a chat
/// page with the screen off doesn't keep using data and battery.
/// </summary>
public sealed class AppVisibility
{
    public bool IsVisible { get; private set; } = true;

    public void Set(bool visible) => IsVisible = visible;
}
