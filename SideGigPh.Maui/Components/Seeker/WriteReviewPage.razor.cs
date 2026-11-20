using Microsoft.AspNetCore.Components;
using SideGigPh.Contracts;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Seeker;

/// <summary>
/// Rate a pro: stars, an optional comment, and the name shown on the review (remembered for
/// next time). Full screen like an interview step. Only for pros the Seeker contacted at least
/// 24 hours ago (<see cref="SeekerWorkspace.CanRate"/>); an
/// earlier review of the same pro is loaded so it can be changed.
/// </summary>
public partial class WriteReviewPage : ComponentBase
{
    [Inject] private SeekerWorkspace Workspace { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;
    [Inject] private BusyIndicator Busy { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    [Parameter] public string Id { get; set; } = string.Empty;

    /// <summary>Opened from a completed job's card: the review is blind (hidden until the worker reviews too, or 14 days).</summary>
    [SupplyParameterFromQuery(Name = "job")] public bool AfterJob { get; set; }

    private static int BlindDays => (int)ReviewRules.BlindFor.TotalDays;

    private ProfessionalListing? _pro;
    private int _rating;
    private string? _comment;
    private string? _name;
    private bool _busy;

    private bool CanSubmit => _rating > 0 && (_name?.Trim().Length ?? 0) >= 2 && !_busy && !RudeWords.In(_comment) && !RudeWords.In(_name);

    protected override async Task OnInitializedAsync()
    {
        if (!await Workspace.EnsureLoadedAsync())
        {
            Nav.NavigateTo("/", replace: true);
            return;
        }

        if (!Workspace.CanRate(Id) || Workspace.ThreadWith(Id)?.Pro is not { } pro)
        {
            Nav.NavigateTo(SeekerRoutes.Reviews, replace: true);
            return;
        }

        var earlier = Workspace.MyReviewOf(pro.Id);
        _rating = earlier?.Rating ?? 0;
        _comment = earlier?.Comment;
        _name = earlier?.ReviewerName ?? Workspace.Answers.ReviewerName;
        _pro = pro;
    }

    private async Task SubmitAsync()
    {
        if (_busy)
        {
            return; // a second tap before the button re-rendered as disabled
        }

        _busy = true;
        try
        {
            if (await Busy.RunAsync(() => Workspace.SubmitReviewAsync(_pro!, _rating, _comment, _name!)))
            {
                Nav.NavigateTo(SeekerRoutes.Reviews, replace: true);
            }
        }
        finally
        {
            _busy = false;
        }
    }

    private void GoBack() => Nav.NavigateTo(SeekerRoutes.Reviews);
}
