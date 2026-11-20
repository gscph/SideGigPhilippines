using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using SideGigPh.Maui.Components.Seeker;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Search;

/// <summary>
/// A worker's work photos full screen, opened by tapping one of the small photos on their profile
/// (<see cref="SeekerRoutes.ProPhotos"/>): one photo at a time, whole (not cropped), swipe or the arrows for the
/// others, "2 of 5" on top. X (and the phone's Back) returns to the profile as it was.
/// </summary>
public partial class WorkPhotosPage : ComponentBase
{
    [Inject] private SeekerWorkspace Workspace { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;
    [Inject] private BusyIndicator Busy { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;

    [Parameter] public string Id { get; set; } = string.Empty;

    /// <summary>The photo to open on (0 = first); one out of range opens the nearest.</summary>
    [Parameter] public int Index { get; set; }

    private IReadOnlyList<string> _photos = [];
    private int _selected;
    private bool _leaving;

    private string? Title => _photos.Count > 1 ? T.Format(Copy.WorkPhotoCount, _selected + 1, _photos.Count) : null;

    protected override async Task OnParametersSetAsync()
    {
        // The profile just loaded this listing, so it usually comes from the workspace's cache.
        await Busy.RunAsync(async () => _photos = (await Workspace.GetProAsync(Id))?.WorkPhotoUrls ?? []);
        _selected = _photos.Count == 0 ? 0 : Math.Clamp(Index, 0, _photos.Count - 1);
    }

    // Back in the history, so the profile keeps where it came from (its own Back); straight to it if there is none.
    private async Task CloseAsync()
    {
        if (_leaving)
        {
            return;
        }

        _leaving = true;
        try
        {
            await JS.InvokeVoidAsync("history.back");
        }
        catch (JSException)
        {
            Nav.NavigateTo(SeekerRoutes.Pro(Id));
        }
    }
}
