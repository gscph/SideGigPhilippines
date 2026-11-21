using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.JobPosts;

/// <summary>
/// The household's exact place on the picked-job card (<see cref="MeetLocation"/>). The household gets "Send my
/// location": the tap reads the phone's precise location (the permission prompt comes only from this tap, and only
/// when the app doesn't have it yet; a rough point is refused with a way to Settings), sends it to the picked worker,
/// raises <see cref="OnChanged"/> with the job and <see cref="OnShared"/>, so the page adds "Customer's location" to the
/// chat and the worker's phone rings. The worker sees a small map (<see cref="MapFrame"/>), how far and which way in a
/// straight line from where the phone is (read by itself when location is already allowed, else on "Show how far I
/// am"; never sent anywhere), and one button that opens the place in Google Maps or Waze, whichever is installed.
/// </summary>
public partial class MeetLocationSection : ComponentBase
{
    [Inject] private JobBoard Board { get; set; } = default!;
    [Inject] private IDeviceLocation Location { get; set; } = default!;
    [Inject] private IMapLauncher Maps { get; set; } = default!;
    [Inject] private ILogger<MeetLocationSection> Logger { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;

    [Parameter, EditorRequired] public PickedJob Job { get; set; } = default!;

    /// <summary>The other person's name, as the chat shows it.</summary>
    [Parameter, EditorRequired] public string OtherName { get; set; } = string.Empty;

    [Parameter] public EventCallback<PickedJob> OnChanged { get; set; }

    /// <summary>The place was sent: the page adds "Customer's location" to the chat, which rings the worker.</summary>
    [Parameter] public EventCallback OnShared { get; set; }

    private bool _busy;
    private bool _triedQuietly;
    private GeoPoint? _mine;
    private LocationOutcome? _problem;
    private string? _error;

    private LocalizedText? Problem => _problem switch
    {
        LocationOutcome.Denied => Copy.LocationDenied,
        LocationOutcome.Off => Copy.LocationOff,
        LocationOutcome.Rough => Copy.LocationRough,
        LocationOutcome.Abroad => Copy.LocationAbroad,
        LocationOutcome.Failed => Copy.LocationFailed,
        _ => null,
    };

    protected override async Task OnParametersSetAsync()
    {
        // The worker's distance: read by itself only when location is already allowed (no prompt without a tap).
        if (!MeetLocation.CanSee(Job) || _mine is not null || _triedQuietly)
        {
            return;
        }

        _triedQuietly = true;
        try
        {
            if (await Location.IsAllowedAsync() && await ReadAsync() is { } here)
            {
                _mine = here;
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Reading the worker's location by itself failed.");
        }
    }

    private async Task SendAsync()
    {
        if (_busy)
        {
            return; // a second tap before the button re-rendered as disabled
        }

        _busy = true;
        _problem = null;
        _error = null;
        try
        {
            if (await ReadAsync(showProblem: true) is not { } point)
            {
                return;
            }

            if (await Board.ShareMeetLocationAsync(Job, point) is { } sent)
            {
                await OnChanged.InvokeAsync(sent);
                await OnShared.InvokeAsync();
            }
            else
            {
                _error = T.Get(Copy.MeetSendFailed);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Sending the household's location failed.");
            _error = T.Get(Copy.MeetSendFailed);
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task HowFarAsync()
    {
        if (_busy)
        {
            return; // a second tap before the button re-rendered as disabled
        }

        _busy = true;
        _problem = null;
        try
        {
            _mine = await ReadAsync(showProblem: true);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Reading the worker's location failed.");
            _problem = LocationOutcome.Failed;
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task OpenAsync(GeoPoint place)
    {
        _error = null;
        if (!await Maps.OpenAsync(place, T.Format(Copy.PinLabel, OtherName)))
        {
            _error = T.Get(Copy.MeetOpenFailed);
        }
    }

    /// <summary>The phone's precise point, or null (with the reason in <see cref="_problem"/> when asked to show it).</summary>
    private async Task<GeoPoint?> ReadAsync(bool showProblem = false)
    {
        var fix = await Location.GetCurrentAsync();
        var outcome = WorkArea.Read(fix);
        if (outcome == LocationOutcome.Set && fix.Point is { } point)
        {
            return point;
        }

        if (showProblem)
        {
            _problem = outcome;
        }

        return null;
    }
}
