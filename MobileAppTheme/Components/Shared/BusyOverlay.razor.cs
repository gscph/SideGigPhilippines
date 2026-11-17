using Microsoft.AspNetCore.Components;
using MobileAppTheme.Localization;
using MobileAppTheme.Services;

namespace MobileAppTheme.Components.Shared;

/// <summary>
/// Full-screen "please wait" while <see cref="BusyIndicator"/> is busy: the brand <see cref="SplashScreen"/> on a
/// solid background that blocks taps, with a line under the logo that changes every couple of seconds. The lines
/// come from <see cref="ISplashLines"/> (the app picks them, e.g. by the user's role); the first one is the main line,
/// shown first every time and held for <see cref="FirstLineFor"/>, so a slow network call reads
/// as the app at work, not a frozen screen. Hosted once in <c>MainLayout</c>; quick calls finish before
/// <see cref="ShowAfter"/> and never flash it. A wait longer than <see cref="SlowAfter"/> says so instead: "Slow connection.
/// Still working on it…", or "No internet. Waiting…" while the phone is offline (<see cref="INetworkStatus"/>).
/// </summary>
public partial class BusyOverlay : ComponentBase, IDisposable
{
    /// <summary>Calls shorter than this never show the overlay.</summary>
    public static readonly TimeSpan ShowAfter = TimeSpan.FromMilliseconds(300);

    /// <summary>How long the first (main) line stays before the others take turns.</summary>
    public static readonly TimeSpan FirstLineFor = TimeSpan.FromSeconds(3);

    /// <summary>How long each of the other lines stays before the next one.</summary>
    public static readonly TimeSpan LineEvery = TimeSpan.FromMilliseconds(1800);

    /// <summary>After this long the rotating lines give way to a plain "slow connection" line.</summary>
    public static readonly TimeSpan SlowAfter = TimeSpan.FromSeconds(12);

    [Inject] private BusyIndicator Busy { get; set; } = default!;
    [Inject] private ISplashLines Lines { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;
    [Inject] private INetworkStatus Network { get; set; } = default!;
    [Inject] private TimeProvider Clock { get; set; } = default!;

    private CancellationTokenSource? _cts;
    private bool _visible;
    private IReadOnlyList<LocalizedText> _lines = [];
    private int _line;
    private DateTimeOffset _shownAt;

    private bool IsSlow => _visible && Clock.GetUtcNow() - _shownAt >= SlowAfter;

    private string? Line => IsSlow
        ? T.Get(Network.IsOnline ? CommonCopy.BusySlow : CommonCopy.BusyOffline)
        : _lines.Count > 0 ? T.Get(_lines[_line]) : null;

    protected override void OnInitialized() => Busy.Changed += OnBusyChanged;

    private void OnBusyChanged() => _ = InvokeAsync(async () =>
    {
        if (Busy.IsBusy)
        {
            await ShowAsync();
        }
        else
        {
            Hide();
        }
    });

    private async Task ShowAsync()
    {
        if (_cts is not null)
        {
            return;
        }

        var cts = _cts = new CancellationTokenSource();
        try
        {
            await Task.Delay(ShowAfter, cts.Token);
            _lines = await Lines.LinesAsync();
            _line = 0;
            cts.Token.ThrowIfCancellationRequested();
            _shownAt = Clock.GetUtcNow();
            _visible = true;
            StateHasChanged();
            await Task.Delay(FirstLineFor, cts.Token);
            while (true)
            {
                if (_lines.Count > 1)
                {
                    _line = (_line + 1) % _lines.Count;
                }

                if (_lines.Count > 1 || IsSlow)
                {
                    StateHasChanged();
                }

                await Task.Delay(_line == 0 ? FirstLineFor : LineEvery, cts.Token);
            }
        }
        catch (OperationCanceledException)
        {
            // Done waiting: Hide() already took the overlay down.
        }
        finally
        {
            // Only this method disposes its source: Hide() disposing it while this was still waiting for the lines made
            // the next cts.Token throw ObjectDisposedException, which crashed the app (seen during the worker setup).
            if (ReferenceEquals(_cts, cts))
            {
                _cts = null; // ended some other way: the next busy spell starts afresh, and Hide() never cancels a disposed source
            }

            cts.Dispose();
        }
    }

    private void Hide()
    {
        _cts?.Cancel();
        _cts = null;
        if (_visible)
        {
            _visible = false;
            StateHasChanged();
        }
    }

    public void Dispose()
    {
        Busy.Changed -= OnBusyChanged;
        _cts?.Cancel();
    }
}
