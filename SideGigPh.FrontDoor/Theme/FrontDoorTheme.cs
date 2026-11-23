using MudBlazor;

namespace SideGigPh.FrontDoor.Theme;

/// <summary>
/// The landing page's look: the same brand as the app (SideGigPh.Maui/Components/Layout/AppTheme.cs: keep the colors and
/// the logo path in step with it). 60/30/10: white surfaces, ink #222222, purple #5B2A86 as the one accent; money green,
/// star gold; no blue (blue means "verified" in the app). DM Sans. The page can't reference the app's skeleton (it needs
/// MAUI), so the few values live here. Components pick colors through MudBlazor's <c>Color</c> parameters, and
/// frontdoor.css uses the <c>--mud-palette-*</c> variables; never hex anywhere else.
/// </summary>
public static class FrontDoorTheme
{
    /// <summary>The Tarbaho logo, the Folded T (24-unit box, one color at three opacities): the same paths as the app's <c>AppTheme.Logo</c>.</summary>
    public const string Logo = "<path d=\"M4.08 3.6H13.2L11.76 7.92H2.64Z\" /><path fill-opacity=\"0.78\" d=\"M13.2 3.6H21.36L19.92 7.92H11.76Z\" /><path fill-opacity=\"0.62\" d=\"M10.32 7.92H15.12L11.76 20.4H6.96Z\" /><path d=\"M10.32 7.92H15.12L13.68 10.8Z\" />";

    public static MudTheme Theme { get; } = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#5B2A86",
            PrimaryDarken = "#431F63",
            PrimaryLighten = "#7B4BA6",
            PrimaryContrastText = "#ffffff",
            Secondary = "#6a6a6a",
            SecondaryContrastText = "#ffffff",
            Success = "#118C4F",
            Warning = "#FFB400",
            Error = "#d13438",
            Info = "#E8710A",
            Tertiary = "#5B2A86",
            Dark = "#222222",
            DarkContrastText = "#ffffff",
            TextPrimary = "#222222",
            TextSecondary = "#6a6a6a",
            Background = "#ffffff",
            BackgroundGray = "#f7f7f7",
            Surface = "#ffffff",
            AppbarBackground = "#ffffff",
            AppbarText = "#222222",
            LinesDefault = "#ebebeb",
            LinesInputs = "#b0b0b0",
            Divider = "#ebebeb",
            Skeleton = "#ebebeb",
        },
        Typography = new Typography
        {
            Default = new DefaultTypography
            {
                FontFamily = ["DM Sans", "-apple-system", "BlinkMacSystemFont", "Segoe UI", "Roboto", "Helvetica Neue", "Arial", "sans-serif"],
            },
            H2 = new H2Typography { FontSize = "2.5rem", FontWeight = "700", LineHeight = "1.1", LetterSpacing = "-0.03em" },
            H3 = new H3Typography { FontSize = "2rem", FontWeight = "700", LineHeight = "1.15", LetterSpacing = "-0.02em" },
            H4 = new H4Typography { FontSize = "1.75rem", FontWeight = "700", LineHeight = "1.2", LetterSpacing = "-0.02em" },
            H5 = new H5Typography { FontSize = "1.375rem", FontWeight = "600", LineHeight = "1.25", LetterSpacing = "-0.01em" },
            H6 = new H6Typography { FontSize = "1.125rem", FontWeight = "600", LineHeight = "1.3", LetterSpacing = "normal" },
            Subtitle1 = new Subtitle1Typography { FontSize = "1rem", FontWeight = "600", LineHeight = "1.4" },
            Body1 = new Body1Typography { FontSize = "1rem", LineHeight = "1.5", LetterSpacing = "normal" },
            Body2 = new Body2Typography { FontSize = "0.875rem", LineHeight = "1.45", LetterSpacing = "normal" },
            Caption = new CaptionTypography { FontSize = "0.8125rem", LineHeight = "1.4", LetterSpacing = "normal" },
            Button = new ButtonTypography { FontSize = "1rem", FontWeight = "600", TextTransform = "none", LetterSpacing = "normal" },
        },
        LayoutProperties = new LayoutProperties { DefaultBorderRadius = "8px" },
    };
}
