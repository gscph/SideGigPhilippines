using MudBlazor;

namespace MobileAppTheme.Branding;

/// <summary>
/// Builds the app's <see cref="MudTheme"/> from a <see cref="BrandStyle"/>: the palette from the
/// brand's colors, plus the skeleton's mobile typography. The type is clean and minimal (Airbnb
/// style): system font, tight bold titles (<c>Typo.h4</c> is the page / question title), 16px body
/// and buttons, no upper case. <c>MainLayout</c> hands the theme to <c>MudThemeProvider</c>, which
/// emits it as <c>--mud-palette-*</c> CSS variables. Components pick colors only through MudBlazor's
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

            // Secondary is the muted gray, so Color.Secondary is the "helper text" color.
            Secondary = style.Muted,
            SecondaryContrastText = style.OnPrimary,

            Info = style.Primary,
            Success = style.Success,
            Warning = style.Warning,
            Error = style.Error,

            // Dark is the ink: text-weight icons, own chat bubbles, avatars. Primary is the accent: main buttons, active tab, selected states.
            Dark = style.Ink,
            DarkContrastText = style.Background,

            TextPrimary = style.Ink,
            TextSecondary = style.Muted,

            Background = style.Background,
            BackgroundGray = style.BackgroundGray,
            Surface = style.Background,
            AppbarBackground = style.Background,
            AppbarText = style.Ink,

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
            H2 = new H2Typography { FontSize = "3.5rem", FontWeight = "700", LineHeight = "1.1", LetterSpacing = "-0.03em" },
            H3 = new H3Typography { FontSize = "2.5rem", FontWeight = "700", LineHeight = "1.15", LetterSpacing = "-0.02em" },
            H4 = new H4Typography { FontSize = "1.75rem", FontWeight = "700", LineHeight = "1.2", LetterSpacing = "-0.02em" },
            H5 = new H5Typography { FontSize = "1.375rem", FontWeight = "600", LineHeight = "1.25", LetterSpacing = "-0.01em" },
            H6 = new H6Typography { FontSize = "1.125rem", FontWeight = "600", LineHeight = "1.3", LetterSpacing = "normal" },
            Subtitle1 = new Subtitle1Typography { FontSize = "1rem", FontWeight = "600", LineHeight = "1.4" },
            Subtitle2 = new Subtitle2Typography { FontSize = "0.875rem", FontWeight = "600", LineHeight = "1.4" },
            Body1 = new Body1Typography { FontSize = "1rem", LineHeight = "1.5", LetterSpacing = "normal" },
            Body2 = new Body2Typography { FontSize = "0.875rem", LineHeight = "1.45", LetterSpacing = "normal" },
            Caption = new CaptionTypography { FontSize = "0.75rem", LineHeight = "1.35", LetterSpacing = "normal" },
            Button = new ButtonTypography { FontSize = "1rem", FontWeight = "600", TextTransform = "none", LetterSpacing = "normal" },
        },

        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = style.CornerRadius,
        },
    };
}
