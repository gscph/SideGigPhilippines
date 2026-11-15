using MudBlazor;
using SideGigPh.Maui.Services.Diagnostics;

namespace SideGigPh.Maui.Components.Layout;

/// <summary>
/// The app's brand (Tarbaho; the code keeps the SideGigPh name): the single source of truth for its name, logo, colors and corner radius
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
        // DM Sans: a round, friendly geometric sans close to Airbnb's Cereal, shipped in wwwroot/fonts (OFL) so it
        // works offline from the first launch; the phone's font covers anything it lacks (₱).
        FontFamily = ["DM Sans", "-apple-system", "BlinkMacSystemFont", "Segoe UI", "Roboto", "Helvetica Neue", "Arial", "sans-serif"],
    };

    /// <summary>The logo: a crew of three workers in hard hats with a hammer and a wrench raised behind them (our own drawing, 24-unit box), drawn in <c>Color.Primary</c>.
    /// Same path as Resources/AppIcon/appiconfg.svg, Resources/Splash/splash.svg, the splash in wwwroot/index.html
    /// and Platforms/Android/Resources/drawable/ic_notification.xml.</summary>
    private const string Logo = "<path fill-rule=\"evenodd\" clip-rule=\"evenodd\" d=\"M8.4 7.2A3.6 3.6 0 0 1 11.2 3.69V3.3A0.4 0.4 0 0 1 11.6 2.9H12.4A0.4 0.4 0 0 1 12.8 3.3V3.69A3.6 3.6 0 0 1 15.6 7.2H15.9A0.6 0.6 0 0 1 15.9 8.4H8.1A0.6 0.6 0 0 1 8.1 7.2ZM9 9.1H15V10A3 3 0 0 1 9 10ZM5.5 21V18.2A4 4 0 0 1 9.5 14.2H14.5A4 4 0 0 1 18.5 18.2V21ZM10.6 14.2L12 16.4L13.4 14.2ZM2.1 9.4A2.5 2.5 0 0 1 4 6.97V6.7A0.3 0.3 0 0 1 4.3 6.4H4.9A0.3 0.3 0 0 1 5.2 6.7V6.97A2.5 2.5 0 0 1 7.1 9.4H7.25A0.45 0.45 0 0 1 7.25 10.3H1.95A0.45 0.45 0 0 1 1.95 9.4ZM2.6 11H6.6V11.4A2 2 0 0 1 2.6 11.4ZM1 21V17.6A3 3 0 0 1 4 14.6H6.33A4.8 4.8 0 0 0 4.7 18.2V21ZM16.9 9.4A2.5 2.5 0 0 1 18.8 6.97V6.7A0.3 0.3 0 0 1 19.1 6.4H19.7A0.3 0.3 0 0 1 20 6.7V6.97A2.5 2.5 0 0 1 21.9 9.4H22.05A0.45 0.45 0 0 1 22.05 10.3H16.75A0.45 0.45 0 0 1 16.75 9.4ZM17.4 11H21.4V11.4A2 2 0 0 1 17.4 11.4ZM23 21V17.6A3 3 0 0 0 20 14.6H17.67A4.8 4.8 0 0 1 19.3 18.2V21ZM4.92 0.81L6.19 2.08L5.03 3.25L7.79 6.01L7.01 6.79L4.25 4.03L3.08 5.19L1.81 3.92ZM21.9 2.1L20.64 3.35L19.65 2.36L20.9 1.1A2.1 2.1 0 0 0 18.18 4.04L16.21 6.01L16.99 6.79L18.96 4.82A2.1 2.1 0 0 0 21.9 2.1Z\"/>";

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
        Version: BuildInfo.Current.Label);
}
