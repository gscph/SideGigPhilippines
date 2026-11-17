using Microsoft.AspNetCore.Components;
using MudBlazor;
using MobileAppTheme.Localization;

namespace MobileAppTheme.Components.Shared;

/// <summary>Which end of the page a list grows at.</summary>
public enum LoadMoreEdge
{
    /// <summary>A feed or results: more come under the list as the person scrolls down (swipes up).</summary>
    Bottom,

    /// <summary>A chat: the page opens at its end (newest), and older items come above as the person scrolls up (swipes down).</summary>
    Top,
}

/// <summary>
/// Put at the growing end of a long list (last for <see cref="LoadMoreEdge.Bottom"/>, first for
/// <see cref="LoadMoreEdge.Top"/>). While <see cref="HasMore"/> is true it shows a small spinner and raises
/// <see cref="OnLoadMore"/> when the page is scrolled near that end, or right away when the list is too short to
/// scroll. A load that adds nothing (no signal, or the server took too long) swaps the spinner for "Couldn't load more"
/// and a Try again button, since a spinner that never moves leaves the person stuck. For <see cref="LoadMoreEdge.Top"/> it first scrolls the page to its end, and keeps what the person was
/// reading in place when older items are added above. Uses MudBlazor's scroll listener and scroll manager on the page
/// itself, so no JavaScript of our own.
/// </summary>
public sealed partial class LoadMoreOnScroll : ComponentBase, IAsyncDisposable
{
    /// <summary>How close to the end (px) counts as "at the end": about two list rows.</summary>
    private const double NearEndPx = 300;

    /// <summary>How often the scroll position is reported while scrolling.</summary>
    private const int ReportRateMs = 100;

    /// <summary>The page's scrolling element, for MudBlazor's scroll manager.</summary>
    private const string Page = "html";

    [Inject] private IScrollListenerFactory ScrollListeners { get; set; } = default!;
    [Inject] private IScrollManager Scroll { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;

    /// <summary>Whether there is more to load; the spinner and the listener stop when false.</summary>
    [Parameter, EditorRequired] public bool HasMore { get; set; }

    /// <summary>Loads the next page; the parent adds it and re-renders.</summary>
    [Parameter, EditorRequired] public EventCallback OnLoadMore { get; set; }

    [Parameter] public LoadMoreEdge Edge { get; set; } = LoadMoreEdge.Bottom;

    private IScrollListener? _listener;
    private bool _loading;

    /// <summary>The last load added nothing: Try again shows instead of the spinner, and scrolling doesn't load.</summary>
    private bool _stalled;

    /// <summary>The page height when the last load started; a load that added nothing (offline) waits for the next scroll.</summary>
    private double? _heightAtLastLoad;

    /// <summary>Where the page was when an older page was asked for (<see cref="LoadMoreEdge.Top"/>), to keep the place.</summary>
    private ScrollEventArgs? _beforeLoad;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            if (Edge == LoadMoreEdge.Top)
            {
                await Scroll.ScrollToBottomAsync(Page, ScrollBehavior.Auto);
            }

            _listener = ScrollListeners.Create(null, ReportRateMs);
            _listener.OnScroll += OnScroll;
        }

        if (_listener is null || await _listener.GetCurrentScrollDataAsync() is not { } position)
        {
            return;
        }

        if (_beforeLoad is { } before)
        {
            // Older items went in above: move down by what they added, so the same message stays in view.
            _beforeLoad = null;
            var top = (int)(before.ScrollTop + position.ScrollHeight - before.ScrollHeight);
            if (Math.Abs(position.ScrollTop - top) > 1)
            {
                await Scroll.ScrollToAsync(Page, 0, top, ScrollBehavior.Auto);
            }

            return;
        }

        // A short list never scrolls, so check whether its end is already in view, unless the last load added nothing.
        if (position.ScrollHeight != _heightAtLastLoad)
        {
            await LoadIfNearEndAsync(position);
        }
    }

    private void OnScroll(object? sender, ScrollEventArgs position) =>
        _ = InvokeAsync(() => LoadIfNearEndAsync(position));

    private async Task LoadIfNearEndAsync(ScrollEventArgs position)
    {
        var nearEnd = Edge == LoadMoreEdge.Top
            ? position.ScrollTop <= NearEndPx
            : position.ScrollTop + position.ClientHeight >= position.ScrollHeight - NearEndPx;
        if (!HasMore || _loading || _stalled || !nearEnd)
        {
            return;
        }

        await LoadAsync(position);
    }

    /// <summary>Try again after a load that added nothing.</summary>
    private async Task RetryAsync()
    {
        if (_loading || _listener is null || await _listener.GetCurrentScrollDataAsync() is not { } position)
        {
            return;
        }

        _stalled = false;
        await LoadAsync(position);
    }

    private async Task LoadAsync(ScrollEventArgs position)
    {
        _loading = true;
        _beforeLoad = Edge == LoadMoreEdge.Top ? position : null;
        try
        {
            await OnLoadMore.InvokeAsync();
        }
        finally
        {
            _loading = false;
            _heightAtLastLoad = position.ScrollHeight;
        }

        // Nothing was added though there is more: the page didn't load.
        if (HasMore && _listener is not null && await _listener.GetCurrentScrollDataAsync() is { } after
            && after.ScrollHeight == position.ScrollHeight)
        {
            _stalled = true;
            StateHasChanged();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_listener is not null)
        {
            _listener.OnScroll -= OnScroll;
            if (_listener is IAsyncDisposable disposable)
            {
                await disposable.DisposeAsync();
            }
        }
    }
}
