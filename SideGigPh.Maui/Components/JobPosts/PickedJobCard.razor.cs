using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.JobPosts;

/// <summary>
/// The picked job on a chat page (verified reviews, brief item 12): what was picked and for how much, where each side
/// stands, and the one thing to do next: **Mark done** until this person did; then, once both did, the household's
/// review (<see cref="RateHref"/>) or the worker's stars for the household, right here. Raises
/// <see cref="OnChanged"/> with the job as the server now has it.
/// </summary>
public partial class PickedJobCard : ComponentBase
{
    [Inject] private JobBoard Board { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;

    [Parameter, EditorRequired] public PickedJob Job { get; set; } = default!;

    /// <summary>The other person's name, as the chat shows it.</summary>
    [Parameter, EditorRequired] public string OtherName { get; set; } = string.Empty;

    /// <summary>The other person's uid (on the worker's side, the household the stars are for).</summary>
    [Parameter, EditorRequired] public string OtherUid { get; set; } = string.Empty;

    /// <summary>The household's review page for this worker (null on the worker's side).</summary>
    [Parameter] public string? RateHref { get; set; }

    [Parameter] public EventCallback<PickedJob> OnChanged { get; set; }

    private bool _busy;
    private bool _editing;
    private int _stars;
    private string? _comment;
    private string? _error;

    private string Title => Job.IAmTheHousehold
        ? T.Format(Copy.PickedTitleHousehold, OtherName)
        : T.Format(Copy.PickedTitleWorker, OtherName);

    private string ServiceLabel => (ReferenceData.FindService(Job.ServiceId) ?? ReferenceData.FindTrade(Job.ServiceId)) is { } service ? T.Get(service.Label) : Job.ServiceId;

    private string Status =>
        Job.IsCompleted ? T.Get(Copy.PickedCompleted)
        : Job.DoneByMe ? T.Format(Copy.PickedWaiting, OtherName)
        : (Job.IAmTheHousehold ? Job.DoneByWorker : Job.DoneByHousehold) ? T.Format(Copy.PickedOtherDone, OtherName)
        : T.Get(Copy.PickedNeitherDone);

    private void Edit()
    {
        _stars = Job.MyRating ?? 0;
        _editing = true;
    }

    private async Task MarkDoneAsync()
    {
        if (_busy)
        {
            return; // a second tap before the button re-rendered as disabled
        }

        _busy = true;
        _error = null;
        try
        {
            if (await Board.MarkDoneAsync(Job) is { } done)
            {
                await OnChanged.InvokeAsync(done);
            }
            else
            {
                _error = T.Get(Copy.PickedDidNotSend);
            }
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task RateAsync()
    {
        if (_busy || _stars == 0)
        {
            return; // a second tap before the button re-rendered as disabled
        }

        _busy = true;
        _error = null;
        try
        {
            if (await Board.RateHouseholdAsync(OtherUid, _stars, _comment))
            {
                _editing = false;
                await OnChanged.InvokeAsync(Job with { MyRating = _stars });
            }
            else
            {
                _error = T.Get(Copy.PickedDidNotSend);
            }
        }
        finally
        {
            _busy = false;
        }
    }
}
