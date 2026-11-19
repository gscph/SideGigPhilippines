using MudBlazor;
using SideGigPh.Maui.Services.Diagnostics;

namespace SideGigPh.Maui.Components.Layout;

/// <summary>
/// The app's brand (Tarbaho; the code keeps the SideGigPh name): the single source of truth for its name, logo, colors and corner radius
/// (60/30/10: white surfaces, near-black ink #222222 for text and icons, deep purple #5B2A86 as the
/// one accent for the logo, main buttons, the active tab, progress and selected states).
/// Four fixed exceptions, never used for anything else: stars are gold (<c>Color.Warning</c>, #FFB400); money is
/// dollar green (<c>Color.Success</c>, #118C4F; also "published" and done checks); a fully set-up worker profile is
/// verified blue (<c>Color.Tertiary</c>, #1877F2, the only blue in the app, like a verified check); and the nudge to
/// add a badge is amber (<c>Color.Info</c>, #E8710A). The MobileAppTheme skeleton turns the colors into the
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
        Info = "#E8710A",
        Tertiary = "#1877F2",
        Background = "#ffffff",
        BackgroundGray = "#f7f7f7",
        Lines = "#ebebeb",
        LinesInputs = "#b0b0b0",
        CornerRadius = "8px",
        // DM Sans: a round, friendly geometric sans close to Airbnb's Cereal, shipped in wwwroot/fonts (OFL) so it
        // works offline from the first launch; the phone's font covers anything it lacks (₱).
        FontFamily = ["DM Sans", "-apple-system", "BlinkMacSystemFont", "Segoe UI", "Roboto", "Helvetica Neue", "Arial", "sans-serif"],
    };

    /// <summary>The logo: the Folded T, a slanted T for Tarbaho folded like a ribbon (our own drawing, 24-unit box), drawn in
    /// <c>Color.Primary</c> at three strengths (full, 78%, 62%), so it stays one theme color and still reads as three shades.
    /// Same shapes as Resources/AppIcon/appiconfg.svg, Resources/Splash/splash.svg, the splash in wwwroot/index.html
    /// and Platforms/Android/Resources/drawable/ic_notification.xml.</summary>
    private const string Logo = "<path d=\"M4.08 3.6H13.2L11.76 7.92H2.64Z\" /><path fill-opacity=\"0.78\" d=\"M13.2 3.6H21.36L19.92 7.92H11.76Z\" /><path fill-opacity=\"0.62\" d=\"M10.32 7.92H15.12L11.76 20.4H6.96Z\" /><path d=\"M10.32 7.92H15.12L13.68 10.8Z\" />";

    /// <summary>
    /// The public legal pages (privacy policy, terms, data and account deletion), served by the API's one public HTML
    /// route (<c>GET legal/{page}</c>, from <c>docs/legal/</c>). The same addresses go in Play Console and App Store Connect.
    /// </summary>
    public static readonly LegalLinks Legal = new(
        PrivacyUrl: MauiProgram.ApiUrl + "legal/privacy",
        TermsUrl: MauiProgram.ApiUrl + "legal/terms",
        DataUrl: MauiProgram.ApiUrl + "legal/data");

    public static AppBrand Brand { get; } = new(
        "Tarbaho",
        Logo,
        MobileTheme.Create(Style),
        Tagline: new LocalizedText(
            "Offer your skills, or find a Kuya or Ate nearby.",
            "Umextra para sa Pamilya — o maghanap ng Kuya o Ate malapit sa'yo."),
        Legal: Legal,
        Version: BuildInfo.Current.Label,
        SplashCredit: new LocalizedText("Powered by AI", "Powered by AI"));
}
