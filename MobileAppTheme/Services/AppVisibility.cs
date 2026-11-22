namespace MobileAppTheme.Services;

/// <summary>
/// Whether the app is on screen. The app's <c>Window</c> sets it from its <c>Stopped</c> / <c>Resumed</c> events;
/// background work (polling an open chat) skips its network calls while the app is hidden, so a phone left on a chat
/// page with the screen off doesn't keep using data and battery. <see cref="TimesHidden"/> lets a web request tell
/// whether the app went to the background while it was on its way: iOS suspends the app and drops its open connections,
/// so such a request fails though the internet is fine.
/// </summary>
public sealed class AppVisibility
{
    private int _timesHidden;

    public bool IsVisible { get; private set; } = true;

    /// <summary>How many times the app has gone to the background since it started.</summary>
    public int TimesHidden => Volatile.Read(ref _timesHidden);

    public void Set(bool visible)
    {
        if (!visible && IsVisible)
        {
            Interlocked.Increment(ref _timesHidden);
        }

        IsVisible = visible;
    }
}
