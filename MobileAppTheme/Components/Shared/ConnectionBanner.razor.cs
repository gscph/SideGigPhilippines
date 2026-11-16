using Microsoft.AspNetCore.Components;
using MobileAppTheme.Localization;
using MobileAppTheme.Services;

namespace MobileAppTheme.Components.Shared;

/// <summary>
/// A strip above every screen (it pushes the page down) while the phone has no internet (<see cref="INetworkStatus"/>), so a screen
/// that can't load reads as "you're offline", not as the app being broken. Sits in <c>MainLayout</c>.
/// </summary>
public partial class ConnectionBanner : ComponentBase, IDisposable
{
    [Inject] private AppText T { get; set; } = default!;
    [Inject] private INetworkStatus Network { get; set; } = default!;

    private bool _online = true;

    protected override void OnInitialized()
    {
        _online = Network.IsOnline;
        Network.Changed += OnChanged;
    }

    private void OnChanged() => _ = InvokeAsync(() =>
    {
        _online = Network.IsOnline;
        StateHasChanged();
    });

    public void Dispose() => Network.Changed -= OnChanged;
}
