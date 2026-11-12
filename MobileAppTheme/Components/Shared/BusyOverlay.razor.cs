using Microsoft.AspNetCore.Components;
using MobileAppTheme.Localization;
using MobileAppTheme.Services;

namespace MobileAppTheme.Components.Shared;

/// <summary>
/// Full-screen "please wait" while <see cref="BusyIndicator"/> is busy: a light veil that blocks taps, a
/// spinner and a short word that changes every couple of seconds ("Thinking…", "Sending…"), so a slow
/// network call reads as progress, not a frozen app. Hosted once in <c>MainLayout</c>; quick calls
/// finish before <see cref="ShowAfter"/> and never flash it.
/// </summary>
public partial class BusyOverlay : ComponentBase, IDisposable
{
    /// <summary>Calls shorter than this never show the overlay.</summary>
    public static readonly TimeSpan ShowAfter = TimeSpan.FromMilliseconds(300);

    /// <summary>How long each word stays before the next one.</summary>
    public static readonly TimeSpan WordEvery = TimeSpan.FromMilliseconds(1800);

    private static readonly IReadOnlyList<LocalizedText> Words =
    [
        CommonCopy.BusyWorking,
        CommonCopy.BusyThinking,
        CommonCopy.BusyProcessing,
        CommonCopy.BusyConnecting,
        CommonCopy.BusySending,
        CommonCopy.BusyChecking,
        CommonCopy.BusySaving,
        CommonCopy.BusyTidying,
        CommonCopy.BusyAlmost,
        CommonCopy.BusyHangTight,
    ];

    [Inject] private BusyIndicator Busy { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;

    private CancellationTokenSource? _cts;
    private bool _visible;
    private int _word;

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
        _word = Random.Shared.Next(Words.Count);
        try
        {
            await Task.Delay(ShowAfter, cts.Token);
            _visible = true;
            StateHasChanged();
            while (true)
            {
                await Task.Delay(WordEvery, cts.Token);
                _word = (_word + 1) % Words.Count;
                StateHasChanged();
            }
        }
        catch (OperationCanceledException)
        {
            // Done waiting: Hide() already took the overlay down.
        }
    }

    private void Hide()
    {
        _cts?.Cancel();
        _cts?.Dispose();
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
        _cts?.Dispose();
    }
}
