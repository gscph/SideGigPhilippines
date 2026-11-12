using MudBlazor;

namespace SideGigPh.Maui.Components.Layout;

/// <summary>
/// SideGigPh's brand: the single source of truth for its name, logo, colors and corner radius
/// (60/30/10: white surfaces, near-black ink #222222 for text and icons, deep purple #5B2A86 as the
/// one accent for the logo, main buttons, the active tab, progress and selected states; no blue).
/// Two fixed exceptions: stars are gold (<c>Color.Warning</c>, #FFB400) and money is dollar green
/// (<c>Color.Success</c>, #118C4F; also "published" and done checks). The MobileAppTheme skeleton turns the colors into the
/// <c>MudTheme</c> (<see cref="MobileTheme.Create"/>), and its <c>MainLayout</c> hands that to
/// <c>MudThemeProvider</c>, which emits it as <c>--mud-palette-*</c> CSS variables. Components pick
/// colors through MudBlazor's <c>Color</c> parameters; the few rules in app.css use those variables.
/// Never hard-code hex elsewhere. The pre-Blazor splash fallbacks in app.css must match these colors.
/// </summary>
public static class AppTheme
{
    public static BrandStyle Style { get; } = new()
    {
        Primary = "#5B2A86",
        PrimaryDarken = "#431F63",
        PrimaryLighten = "#7B4BA6",
        OnPrimary = "#ffffff",
        Ink = "#222222",
        Muted = "#6a6a6a",
        Success = "#118C4F",
        Warning = "#FFB400",
        Error = "#d13438",
        Background = "#ffffff",
        BackgroundGray = "#f7f7f7",
        Lines = "#ebebeb",
        LinesInputs = "#b0b0b0",
        CornerRadius = "8px",
    };

    public static AppBrand Brand { get; } = new(
        "SideGigPh",
        Icons.Material.Filled.Place,
        MobileTheme.Create(Style),
        Tagline: new LocalizedText(
            "Find trusted skilled workers near you — or earn with your skills.",
            "Maghanap ng maaasahang skilled worker — o kumita gamit ang skills mo."));
}
