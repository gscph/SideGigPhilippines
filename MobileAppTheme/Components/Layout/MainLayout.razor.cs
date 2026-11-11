using Microsoft.AspNetCore.Components;
using MobileAppTheme.Branding;

namespace MobileAppTheme.Components.Layout;

/// <summary>
/// The root layout of every page: MudBlazor's theme (from the app's <see cref="AppBrand"/>) and
/// popover providers, then the page. Full-screen pages use it directly; tabbed pages use a side
/// layout that renders <see cref="AppShell"/> and declares <c>@layout MainLayout</c>.
/// </summary>
public partial class MainLayout : LayoutComponentBase
{
    [Inject] private AppBrand Brand { get; set; } = default!;
}
