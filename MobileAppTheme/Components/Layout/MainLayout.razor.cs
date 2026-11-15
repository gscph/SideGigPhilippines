using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using MobileAppTheme.Branding;
using MobileAppTheme.Localization;

namespace MobileAppTheme.Components.Layout;

/// <summary>
/// The root layout of every page: MudBlazor's theme (from the app's <see cref="AppBrand"/>) and
/// popover providers, then the page. Full-screen pages use it directly; tabbed pages use a side
/// layout that renders <see cref="AppShell"/> and declares <c>@layout MainLayout</c>.
/// A page that throws is caught by <see cref="AppErrorBoundary"/> (logged, so it is reported) and replaced by a
/// recover screen that goes back to <c>/</c>; moving to another page also clears it.
/// </summary>
public partial class MainLayout : LayoutComponentBase, IDisposable
{
    [Inject] private AppBrand Brand { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    private AppErrorBoundary? _boundary;

    protected override void OnInitialized() => Nav.LocationChanged += OnLocationChanged;

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e) => _boundary?.Recover();

    private void StartAgain()
    {
        _boundary?.Recover();
        Nav.NavigateTo("/", replace: true);
    }

    public void Dispose() => Nav.LocationChanged -= OnLocationChanged;
}
