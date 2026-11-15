using Microsoft.AspNetCore.Components;
using MobileAppTheme.Branding;

namespace MobileAppTheme.Components.Shared;

/// <summary>
/// The brand splash: logo and name (<see cref="BrandMark"/>), a line under them, an optional softer second line,
/// a small spinner and the build's version (<see cref="AppBrand.Version"/>, muted). The launch screen shows it while the app starts; <see cref="BusyOverlay"/> shows it while
/// the app waits, changing <see cref="Line"/> every couple of seconds.
/// </summary>
public partial class SplashScreen : ComponentBase
{
    [Inject] private AppBrand Brand { get; set; } = default!;

    [Parameter] public string? Line { get; set; }

    [Parameter] public string? SubLine { get; set; }

    [Parameter] public string? Class { get; set; } = "full-screen";
}
