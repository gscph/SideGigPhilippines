using Microsoft.AspNetCore.Components;
using MobileAppTheme.Branding;

namespace MobileAppTheme.Components.Shared;

/// <summary>The app's logo and name from its <see cref="AppBrand"/>, with an optional tagline under it (splash, welcome screens).</summary>
public partial class BrandMark : ComponentBase
{
    [Inject] private AppBrand Brand { get; set; } = default!;

    [Parameter] public string? Tagline { get; set; }
}
