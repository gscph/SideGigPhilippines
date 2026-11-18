using Microsoft.AspNetCore.Components;
using SideGigPh.Contracts;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.JobPosts;

/// <summary>
/// The form to post what a household needs done, full screen outside the tab shell, laid out like Airbnb's "Review and
/// continue": one card with What (kind of work + exact service, "Others" too), Where (town, the launch's default town
/// when they have none, optional barangay and "Use my location") and Your budget (from–to, the suggested minimum price
/// as a guide only), each changed on its own full-screen sheet (Cancel / Save, <see cref="PostReview"/>); then what is
/// wrong and up to three photos. It starts with the trade, service, town and barangay
/// from their last search or post, so most people only type the problem and a number. Post leads back to their posts.
/// </summary>
public partial class NewPostPage : ComponentBase
{
    [Inject] private JobBoard Board { get; set; } = default!;
    [Inject] private SeekerWorkspace Workspace { get; set; } = default!;
    [Inject] private IPhotoPicker Picker { get; set; } = default!;
    [Inject] private BusyIndicator Busy { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;
    [Inject] private ChatAlerts Alerts { get; set; } = default!;

    private readonly JobPostDraft _draft = new();
    private PhotoAddResult? _photoResult;
    private PostPart? _sheet;
    private PostSnapshot? _before;
    private bool _busy;
    private bool _failed;

    protected override async Task OnInitializedAsync()
    {
        if (!await Workspace.EnsureLoadedAsync())
        {
            Nav.NavigateTo("/", replace: true);
            return;
        }

        var answers = Workspace.Answers;
        _draft.CategoryId = ReferenceData.FindTrade(answers.CategoryId)?.Id;
        _draft.ServiceId = ReferenceData.ServicesOf(_draft.CategoryId).FirstOrDefault(service => service.Id == answers.ServiceId)?.Id;
        _draft.CityId = ReferenceData.Find(ReferenceData.Cities, answers.TownShown())?.Id;
        _draft.BarangayId = ReferenceData.FindBarangay(_draft.CityId, answers.BarangayId)?.Id;
        if (answers.IsOtherTrade())
        {
            _draft.Description = answers.OtherJobText;
        }

        if (BudgetRange.TryParse(answers.BudgetId, out var from, out var upTo))
        {
            (_draft.BudgetMinPesos, _draft.BudgetPesos) = (from > 0 ? from : null, upTo);
        }
    }

    private void GoBack() => Nav.NavigateTo(JobPostRoutes.Mine);

    private void OpenSheet(PostPart part)
    {
        if (_busy)
        {
            return;
        }

        _before = PostReview.Take(_draft);
        _sheet = part;
    }

    /// <summary>Cancel or the close button: the answers as they were when the sheet opened.</summary>
    private void CancelSheet()
    {
        if (_before is not null)
        {
            PostReview.Restore(_draft, _before);
        }

        _sheet = null;
    }

    private void SaveSheet() => _sheet = null;

    // The phone's photo picker shows over the page; the splash covers only the upload.
    private async Task AddPhotosAsync()
    {
        if (_busy)
        {
            return; // a second tap before the button re-rendered as disabled
        }

        _busy = true;
        try
        {
            var pick = await Picker.PickAsync(JobBoard.PhotoRoom(_draft));
            _photoResult = await Busy.RunAsync(() => Board.AddPhotosAsync(_draft, pick));
        }
        finally
        {
            _busy = false;
        }
    }

    private Task RemovePhotoAsync(string photoId) => Board.RemovePhotoAsync(_draft, photoId);

    private async Task PostAsync()
    {
        if (_busy || !JobBoard.CanPost(_draft))
        {
            return; // a second tap before the button re-rendered as disabled
        }

        _busy = true;
        _failed = false;
        try
        {
            if (await Busy.RunAsync(() => Board.PostAsync(_draft)) == JobPostResult.Posted)
            {
                await Workspace.RememberPostAsync(_draft);
                // Now there are offers to wait for: allow notifications (asked once; the system remembers).
                _ = Alerts.AskAsync();
                Nav.NavigateTo(JobPostRoutes.Mine, replace: true);
                return;
            }

            _failed = true;
        }
        finally
        {
            _busy = false;
        }
    }
}
