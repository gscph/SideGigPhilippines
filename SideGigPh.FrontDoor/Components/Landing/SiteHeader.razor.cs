using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace SideGigPh.FrontDoor.Components.Landing;

/// <summary>
/// The sticky white top bar: logo (back to the top), section links on wide screens, the language toggle and a small
/// "Early access" button, hidden while the form itself is on screen so it never points at itself.
/// </summary>
public partial class SiteHeader : IAsyncDisposable
{
    [Inject] private PageScript Script { get; set; } = default!;

    private DotNetObjectReference<SiteHeader>? _listener;
    private bool _formOnScreen;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        _listener = DotNetObjectReference.Create(this);
        await Script.WatchAsync(Sections.EarlyAccess, _listener);
    }

    /// <summary>Called by frontdoor.js as the form scrolls in and out of view.</summary>
    [JSInvokable]
    public void SectionVisible(bool visible)
    {
        if (visible == _formOnScreen)
        {
            return;
        }

        _formOnScreen = visible;
        StateHasChanged();
    }

    private Task ToTopAsync() => Script.ScrollToTopAsync();

    private Task ScrollAsync(string section) => Script.ScrollToAsync(section);

    private Task ToFormAsync() => Script.ScrollToAsync(Sections.EarlyAccess, Sections.EmailFieldSelector);

    public async ValueTask DisposeAsync()
    {
        await Script.UnwatchAsync(Sections.EarlyAccess);
        _listener?.Dispose();
        Dispose();
    }
}
