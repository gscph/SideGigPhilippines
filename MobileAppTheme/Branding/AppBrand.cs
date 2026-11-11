using MudBlazor;

namespace MobileAppTheme.Branding;

/// <summary>
/// What makes the skeleton look like one product: its name (top bar, <c>BrandMark</c>), its logo
/// (a MudBlazor icon, e.g. <c>Icons.Material.Filled.Place</c>) and its <see cref="MudTheme"/>
/// (usually from <see cref="MobileTheme.Create"/>). The app builds one and passes it to
/// <c>AddMobileAppTheme</c>; skeleton components inject it.
/// </summary>
public sealed record AppBrand(string Name, string LogoIcon, MudTheme Theme);
