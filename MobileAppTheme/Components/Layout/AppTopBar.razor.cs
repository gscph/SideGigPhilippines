using Microsoft.AspNetCore.Components;
using MobileAppTheme.Branding;
using MobileAppTheme.Localization;

namespace MobileAppTheme.Components.Layout;

/// <summary>Brand bar across the top of <see cref="AppShell"/>: the logo with the build's version beside it (muted); the user's avatar on the right opens their page.</summary>
public partial class AppTopBar : ComponentBase
{
    [Inject] private AppBrand Brand { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;

    [Parameter, EditorRequired] public string ProfileHref { get; set; } = string.Empty;

    [Parameter] public string? Name { get; set; }

    [Parameter] public string? PhotoUrl { get; set; }

    [Parameter] public bool ShowStatusDot { get; set; }
}
