using MudBlazor;

namespace MobileAppTheme.Branding;

/// <summary>
/// Builds the app's <see cref="MudTheme"/> from a <see cref="BrandStyle"/>: the palette from the
/// brand's colors, plus the skeleton's mobile typography (system font, bold headings, buttons
/// without upper case). <c>MainLayout</c> hands the theme to <c>MudThemeProvider</c>, which emits
/// it as <c>--mud-palette-*</c> CSS variables. Components pick colors only through MudBlazor's
/// <c>Color</c> parameters, never hex.
/// </summary>
public static class MobileTheme
{
    public static MudTheme Create(BrandStyle style) => new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = style.Primary,
            PrimaryDarken = style.PrimaryDarken,
            PrimaryLighten = style.PrimaryLighten,
            PrimaryContrastText = style.OnPrimary,

            // Secondary is the muted slate, so Color.Secondary is the "helper text" color.
            Secondary = style.Muted,
            SecondaryContrastText = style.OnPrimary,

            Info = style.Primary,
            Success = style.Success,
            Warning = style.Warning,
            Error = style.Error,
            Dark = style.Ink,

            TextPrimary = style.Ink,
            TextSecondary = style.Muted,

            Background = style.Background,
            BackgroundGray = style.BackgroundGray,
            Surface = style.Background,
            AppbarBackground = style.Primary,
            AppbarText = style.OnPrimary,

            LinesDefault = style.Lines,
            LinesInputs = style.LinesInputs,
            Divider = style.Lines,
            Skeleton = style.Lines,
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
            DefaultBorderRadius = style.CornerRadius,
        },
    };
}
