using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using SideGigPh.Maui.Services;

namespace SideGigPh.Maui.Components.Professional;

/// <summary>
/// Where the worker works from (<see cref="ProRoutes.WorkArea"/>): their base point from the phone (else the middle of
/// their towns) and how far they travel, which decide the Requests they see. Save keeps both and re-publishes a live
/// listing (<see cref="ProWorkspace.SetWorkAreaAsync"/>), then goes back to Me.
/// </summary>
public partial class WorkAreaPage : ComponentBase
{
    [Inject] private ProWorkspace Workspace { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;
    [Inject] private ILogger<WorkAreaPage> Logger { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;

    /// <summary>A point found on this visit, saved on Save (null keeps the one the worker has).</summary>
    private GeoPoint? _point;
    private int _radiusKm = WorkArea.DefaultRadiusKm;
    private bool _busy;
    private bool _failed;

    protected override void OnInitialized() => _radiusKm = Workspace.Draft.RadiusKm ?? WorkArea.DefaultRadiusKm;

    private async Task SaveAsync()
    {
        if (_busy)
        {
            return; // a second tap before the button re-rendered as disabled
        }

        _busy = true;
        _failed = false;
        try
        {
            await Workspace.SetWorkAreaAsync(_point, _radiusKm);
            Nav.NavigateTo(ProRoutes.Me);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Saving the work area failed.");
            _failed = true;
        }
        finally
        {
            _busy = false;
        }
    }
}
