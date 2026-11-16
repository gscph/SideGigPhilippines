using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Shared;

/// <summary>
/// "Use my location": whether a precise point is set (else the town's centre is used), a button that asks the phone
/// (the permission prompt comes only from this tap, after the reason is on screen) and what went wrong if it didn't
/// work, with a way to Settings when location was refused. Raises <see cref="OnFound"/> with a usable point only
/// (<see cref="WorkArea.Read"/>): never a rough one or one abroad.
/// </summary>
public partial class LocationButton : ComponentBase
{
    [Inject] private IDeviceLocation Location { get; set; } = default!;
    [Inject] private ILogger<LocationButton> Logger { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;

    /// <summary>A precise point is already set.</summary>
    [Parameter] public bool IsSet { get; set; }

    /// <summary>Why the point is asked for and who sees what, under the button.</summary>
    [Parameter] public string? Why { get; set; }

    [Parameter] public bool Disabled { get; set; }

    [Parameter] public EventCallback<GeoPoint> OnFound { get; set; }

    private bool _busy;
    private LocationOutcome? _problem;

    private LocalizedText? Problem => _problem switch
    {
        LocationOutcome.Denied => Copy.LocationDenied,
        LocationOutcome.Off => Copy.LocationOff,
        LocationOutcome.Rough => Copy.LocationRough,
        LocationOutcome.Abroad => Copy.LocationAbroad,
        LocationOutcome.Failed => Copy.LocationFailed,
        _ => null,
    };

    private async Task FindAsync()
    {
        if (_busy)
        {
            return; // a second tap before the button re-rendered as disabled
        }

        _busy = true;
        _problem = null;
        try
        {
            var fix = await Location.GetCurrentAsync();
            var outcome = WorkArea.Read(fix);
            if (outcome == LocationOutcome.Set && fix.Point is { } point)
            {
                await OnFound.InvokeAsync(point);
            }
            else
            {
                _problem = outcome;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Using the phone's location failed.");
            _problem = LocationOutcome.Failed;
        }
        finally
        {
            _busy = false;
        }
    }
}
