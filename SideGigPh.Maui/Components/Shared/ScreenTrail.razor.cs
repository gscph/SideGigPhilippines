using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using SideGigPh.Maui.Services.Diagnostics;

namespace SideGigPh.Maui.Components.Shared;

/// <summary>
/// Tells the <see cref="ErrorLog"/> each screen the person opens (the path only, never the query), so an error report
/// says where it happened and which screens came before. Sits once in <c>Routes.razor</c> and renders nothing.
/// </summary>
public sealed partial class ScreenTrail : ComponentBase, IDisposable
{
    [Inject] private NavigationManager Nav { get; set; } = default!;
    [Inject] private ErrorLog Log { get; set; } = default!;

    protected override void OnInitialized()
    {
        Visit(Nav.Uri);
        Nav.LocationChanged += OnLocationChanged;
    }

    public void Dispose() => Nav.LocationChanged -= OnLocationChanged;

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e) => Visit(e.Location);

    private void Visit(string uri) => Log.Visit("/" + Nav.ToBaseRelativePath(uri).Split('?', '#')[0]);
}
