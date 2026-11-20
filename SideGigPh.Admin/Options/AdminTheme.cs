using MudBlazor;

namespace SideGigPh.Admin.Options;

/// <summary>
/// The operator site's look: the phone app's Tarbaho colors (SideGigPh.Maui <c>AppTheme</c>: purple accent, near-black
/// ink, gold stars, money green) and DM Sans. The one place the site's colors are written.
/// </summary>
public static class AdminTheme
{
    public static readonly MudTheme Theme = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#5B2A86",
            Dark = "#222222",
            TextPrimary = "#222222",
            Secondary = "#717171",
            Success = "#118C4F",
            Warning = "#FFB400",
            Info = "#E8710A",
            Tertiary = "#1877F2",
            AppbarBackground = "#FFFFFF",
            AppbarText = "#222222",
            Background = "#FFFFFF",
        },
        Typography = new Typography
        {
            Default = new DefaultTypography { FontFamily = ["DM Sans", "system-ui", "sans-serif"] },
        },
        LayoutProperties = new LayoutProperties { DefaultBorderRadius = "8px" },
    };
}
