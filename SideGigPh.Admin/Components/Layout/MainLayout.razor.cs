using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using SideGigPh.Admin.Services;

namespace SideGigPh.Admin.Components.Layout;

/// <summary>
/// The frame: an operator gets the top bar, the side menu and the page; anyone else the sign-in screens
/// (<see cref="Auth.Views.OperatorGate"/>). A page that throws is caught here, reported to the Error reports page and
/// replaced by <see cref="SiteErrorPanel"/>; moving to another page recovers.
/// </summary>
public partial class MainLayout : IDisposable
{
    [Inject] public required AuthService Auth { get; set; }

    private ErrorBoundary? _errorBoundary;
    private bool _drawerOpen = true;

    protected override async Task OnInitializedAsync()
    {
        Auth.Changed += OnAuthChanged;
        await Auth.StartAsync();
    }

    protected override void OnParametersSet() => _errorBoundary?.Recover();

    private void ToggleDrawer() => _drawerOpen = !_drawerOpen;

    private void Recover() => _errorBoundary?.Recover();

    private void OnAuthChanged() => InvokeAsync(StateHasChanged);

    public void Dispose() => Auth.Changed -= OnAuthChanged;
}
