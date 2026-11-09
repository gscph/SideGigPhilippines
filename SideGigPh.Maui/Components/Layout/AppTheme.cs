using MudBlazor;

namespace SideGigPh.Maui.Components.Layout;

/// <summary>
/// The app's single source of truth for colors, type and shape (blue / white, GCash /
/// Microsoft style). <see cref="MainLayout"/> hands it to <c>MudThemeProvider</c>, which emits
/// it as <c>--mud-palette-*</c> CSS variables. Components pick colors through MudBlazor's
/// <c>Color</c> parameters; the few rules in app.css use those variables. Never hard-code hex.
/// </summary>
public static class AppTheme
{
    private const string Ink = "#0b1f33";
    private const string Muted = "#5c6b7a";

    public static MudTheme Theme { get; } = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#0078d4",
            PrimaryDarken = "#005a9e",
            PrimaryLighten = "#5ea8e6",
            PrimaryContrastText = "#ffffff",

            // Secondary is the muted slate, so Color.Secondary is the "helper text" color.
            Secondary = Muted,
            SecondaryContrastText = "#ffffff",

            Info = "#0078d4",
            Success = "#107c10",
            Warning = "#c19c00",
            Error = "#d13438",
            Dark = Ink,

            TextPrimary = Ink,
            TextSecondary = Muted,

            Background = "#ffffff",
            BackgroundGray = "#f5f8fc",
            Surface = "#ffffff",
            AppbarBackground = "#0078d4",
            AppbarText = "#ffffff",

            LinesDefault = "#e1e8f0",
            LinesInputs = "#c7d3e0",
            Divider = "#e1e8f0",
            Skeleton = "#e1e8f0",
        },

        Typography = new Typography
        {
            Default = new DefaultTypography
            {
                FontFamily = ["-apple-system", "BlinkMacSystemFont", "Segoe UI", "Roboto", "Helvetica Neue", "Arial", "sans-serif"],
            },
            H4 = new H4Typography { FontWeight = "700" },
            H5 = new H5Typography { FontWeight = "600", LineHeight = "1.25" },
            H6 = new H6Typography { FontWeight = "600" },
            Subtitle1 = new Subtitle1Typography { FontWeight = "600" },
            Button = new ButtonTypography { FontWeight = "600", TextTransform = "none" },
        },

        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "10px",
        },
    };
}
