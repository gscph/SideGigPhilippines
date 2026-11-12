using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Professional;

/// <summary>
/// The pro's work gallery: add photos from the phone, remove any. The newest three show on
/// their listing card. Photos already in the gallery are skipped by hash (anti-fraud rule 2).
/// </summary>
public partial class PhotosPage : ComponentBase
{
    [Inject] private ProWorkspace Workspace { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;
    [Inject] private BusyIndicator Busy { get; set; } = default!;

    private PhotoAddResult? _result;
    private bool _busy;

    private bool IsFull => Workspace.WorkPhotos.Count >= ProWorkspace.MaxWorkPhotos;

    private async Task AddAsync()
    {
        _busy = true;
        try
        {
            _result = await Busy.RunAsync(Workspace.AddWorkPhotosAsync);
        }
        finally
        {
            _busy = false;
        }
    }
}
