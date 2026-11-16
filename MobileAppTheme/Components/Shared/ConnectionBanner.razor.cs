using Microsoft.AspNetCore.Components;
using MobileAppTheme.Localization;
using MobileAppTheme.Services;

namespace MobileAppTheme.Components.Shared;

/// <summary>
/// A gray strip above the page while the phone has no internet (<see cref="INetworkStatus"/>), so a screen that can't
/// load reads as "you're offline", not as the app being broken. It shows only after the phone has been offline for
/// <see cref="ShowAfter"/> (a weak 4G signal drops for a second when switching towers, and a strip popping in and out
/// would move the buttons under the person's finger) and hides as soon as the connection is back. Sits in <c>MainLayout</c>.
/// </summary>
public partial class ConnectionBanner : ComponentBase, IDisposable
{
    /// <summary>How long the phone must stay offline before the strip shows.</summary>
    public static readonly TimeSpan ShowAfter = TimeSpan.FromSeconds(3);

    [Inject] private AppText T { get; set; } = default!;
    [Inject] private INetworkStatus Network { get; set; } = default!;

    private bool _showing;
    private CancellationTokenSource? _wait;

    protected override void OnInitialized()
    {
        Network.Changed += OnChanged;
        OnChanged();
    }

    private void OnChanged() => _ = InvokeAsync(async () =>
    {
        _wait?.Cancel();
        _wait = null;
        if (Network.IsOnline)
        {
            if (_showing)
            {
                _showing = false;
                StateHasChanged();
            }

            return;
        }

        if (_showing)
        {
            return;
        }

        var wait = _wait = new CancellationTokenSource();
        try
        {
            await Task.Delay(ShowAfter, wait.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (!Network.IsOnline)
        {
            _showing = true;
            StateHasChanged();
        }
    });

    public void Dispose()
    {
        Network.Changed -= OnChanged;
        _wait?.Cancel();
    }
}
