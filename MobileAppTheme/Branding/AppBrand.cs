using MobileAppTheme.Localization;
using MudBlazor;

namespace MobileAppTheme.Branding;

/// <summary>
/// What makes the skeleton look like one product: its name (top bar, <c>BrandMark</c>), its logo
/// (a MudBlazor icon, e.g. <c>Icons.Material.Filled.Place</c>), its <see cref="MudTheme"/>
/// (usually from <see cref="MobileTheme.Create"/>) and an optional one-line tagline (shown under
/// the logo on the sign-in screen), and its public legal pages (<see cref="LegalLinks"/>: the sign-in consent line
/// and the Me page link to them), and the build's version label (<c>v0.1.0-rc.12</c>, shown small and muted on the
/// splash and beside the logo in the top bar, so a tester can say which build they run), and an optional small
/// splash credit (<see cref="SplashCredit"/>, e.g. "Powered by …", under the spinner). The app builds one and passes it to <c>AddMobileAppTheme</c>;
/// skeleton components inject it.
/// </summary>
public sealed record AppBrand(string Name, string LogoIcon, MudTheme Theme, LocalizedText? Tagline = null, LegalLinks? Legal = null, string? Version = null, LocalizedText? SplashCredit = null);

/// <summary>
/// The product's public legal pages (web pages anyone can open, as the app stores require). Links to them open in the
/// phone's browser (BlazorWebView opens links outside the app that way).
/// </summary>
/// <param name="PrivacyUrl">The privacy policy.</param>
/// <param name="TermsUrl">The terms of service.</param>
/// <param name="DataUrl">How personal data is handled and how to delete the account without the app.</param>
public sealed record LegalLinks(string PrivacyUrl, string TermsUrl, string DataUrl);
