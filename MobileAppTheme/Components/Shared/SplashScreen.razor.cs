using Microsoft.AspNetCore.Components;

namespace MobileAppTheme.Components.Shared;

/// <summary>
/// The brand splash: logo and name (<see cref="BrandMark"/>), a line under them, an optional softer second line,
/// and a small spinner. The launch screen shows it while the app starts; <see cref="BusyOverlay"/> shows it while
/// the app waits, changing <see cref="Line"/> every couple of seconds.
/// </summary>
public partial class SplashScreen : ComponentBase
{
    [Parameter] public string? Line { get; set; }

    [Parameter] public string? SubLine { get; set; }

    [Parameter] public string? Class { get; set; } = "full-screen";
}
