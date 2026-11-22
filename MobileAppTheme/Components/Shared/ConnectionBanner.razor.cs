using Microsoft.AspNetCore.Components;
using MobileAppTheme.Localization;
using MobileAppTheme.Services;

namespace MobileAppTheme.Components.Shared;

/// <summary>
/// A gray strip above the page while the phone has no internet (<see cref="INetworkStatus"/>), so a screen that can't
/// load reads as "you're offline", not as the app being broken. It shows only after the phone has been offline for
/// <see cref="ShowAfter"/> (a weak 4G signal drops for a second when switching towers, and a strip popping in and out
/// would move the buttons under the person's finger) and hides as soon as the connection is back. While it shows it looks
/// again every <see cref="RecheckEvery"/>: a platform (iOS) can recover without raising a change, and the strip must not
/// stay up on a working connection. Sits in <c>MainLayout</c>.
/// </summary>
public partial class ConnectionBanner : ComponentBase, IDisposable
{
    /// <summary>How long the phone must stay offline before the strip shows.</summary>
    public static readonly TimeSpan ShowAfter = TimeSpan.FromSeconds(3);

    /// <summary>How often the strip, once shown, checks whether the connection is back.</summary>
    public static readonly TimeSpan RecheckEvery = TimeSpan.FromSeconds(5);

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

        var wait = _wait = new CancellationTokenSource();
        try
        {
            if (!_showing)
            {
                await Task.Delay(ShowAfter, wait.Token);
                if (Network.IsOnline)
                {
                    return;
                }

                _showing = true;
                StateHasChanged();
            }

            while (!Network.IsOnline)
            {
                await Task.Delay(RecheckEvery, wait.Token);
            }

            _showing = false;
            StateHasChanged();
        }
        catch (OperationCanceledException)
        {
            // A newer change took over.
        }
    });

    public void Dispose()
    {
        Network.Changed -= OnChanged;
        _wait?.Cancel();
    }
}
