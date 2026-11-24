using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Professional;

/// <summary>
/// Add work photos from the phone's gallery and remove any, on the Photos page: a 2 × 3 grid (<see cref="WorkPhotoGrid"/>)
/// whose empty slots add photos. Optional (0–6), never needed to publish.
/// </summary>
public partial class WorkPhotosEditor : ComponentBase
{
    [Inject] private ProWorkspace Workspace { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;
    [Inject] private BusyIndicator Busy { get; set; } = default!;
    [Inject] private IPhotoPicker Picker { get; set; } = default!;

    /// <summary>The gallery changed (a photo added or removed), so the page can update what depends on the count.</summary>
    [Parameter] public EventCallback OnChanged { get; set; }

    private PhotoAddResult? _result;
    private bool _busy;

    private bool IsFull => Workspace.WorkPhotos.Count >= ProWorkspace.MaxWorkPhotos;

    // The phone's photo picker shows over the page; the splash covers only the upload.
    private async Task AddAsync()
    {
        if (_busy)
        {
            return; // a second tap before the button re-rendered as disabled
        }

        _busy = true;
        try
        {
            var pick = await Picker.PickAsync(Workspace.WorkPhotoRoom);
            _result = await Busy.RunAsync(() => Workspace.AddWorkPhotosAsync(pick));
        }
        finally
        {
            _busy = false;
        }

        await OnChanged.InvokeAsync();
    }

    private async Task RemoveAsync(string photoId)
    {
        await Workspace.RemoveWorkPhotoAsync(photoId);
        await OnChanged.InvokeAsync();
    }
}
