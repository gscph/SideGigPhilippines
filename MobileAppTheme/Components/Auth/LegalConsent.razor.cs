using Microsoft.AspNetCore.Components;
using MobileAppTheme.Branding;
using MobileAppTheme.Localization;

namespace MobileAppTheme.Components.Auth;

/// <summary>
/// "By continuing, you agree to our Terms of Service and Privacy Policy" under the sign-in methods, with both links
/// (<see cref="AppBrand.Legal"/>; nothing when the app has none). Signing in is the agreement (App Store 1.2).
/// </summary>
public partial class LegalConsent : ComponentBase
{
    [Inject] private AppBrand Brand { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;
}
