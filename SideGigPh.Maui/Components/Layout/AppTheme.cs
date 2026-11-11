using MudBlazor;

namespace SideGigPh.Maui.Components.Layout;

/// <summary>
/// SideGigPh's brand: the single source of truth for its name, logo, colors and corner radius
/// (blue / white, GCash / Microsoft style). The MobileAppTheme skeleton turns the colors into the
/// <c>MudTheme</c> (<see cref="MobileTheme.Create"/>), and its <c>MainLayout</c> hands that to
/// <c>MudThemeProvider</c>, which emits it as <c>--mud-palette-*</c> CSS variables. Components pick
/// colors through MudBlazor's <c>Color</c> parameters; the few rules in app.css use those variables.
/// Never hard-code hex elsewhere. The pre-Blazor splash fallbacks in app.css must match these colors.
/// </summary>
public static class AppTheme
{
    public static BrandStyle Style { get; } = new()
    {
        Primary = "#0078d4",
        PrimaryDarken = "#005a9e",
        PrimaryLighten = "#5ea8e6",
        OnPrimary = "#ffffff",
        Ink = "#0b1f33",
        Muted = "#5c6b7a",
        Success = "#107c10",
        Warning = "#c19c00",
        Error = "#d13438",
        Background = "#ffffff",
        BackgroundGray = "#f5f8fc",
        Lines = "#e1e8f0",
        LinesInputs = "#c7d3e0",
        CornerRadius = "10px",
    };

    public static AppBrand Brand { get; } = new(
        "SideGigPh",
        Icons.Material.Filled.Place,
        MobileTheme.Create(Style),
        Tagline: new LocalizedText(
            "Find trusted skilled workers near you — or earn with your skills.",
            "Maghanap ng maaasahang skilled worker — o kumita gamit ang skills mo."));
}
