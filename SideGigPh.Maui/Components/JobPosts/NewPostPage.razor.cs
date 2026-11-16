using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.JobPosts;

/// <summary>
/// The form to post what a household needs done, full screen outside the tab shell: kind of work, the exact
/// service, town, what is wrong, their budget and up to three photos. It starts with the trade, service and town
/// from their last search, so most people only type the problem and a number. Post leads back to their posts.
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
        _draft.CategoryId = ReferenceData.Find(ReferenceData.Categories, answers.CategoryId)?.Id;
        _draft.ServiceId = ReferenceData.ServicesOf(_draft.CategoryId).FirstOrDefault(service => service.Id == answers.ServiceId)?.Id;
        _draft.CityId = ReferenceData.Find(ReferenceData.Cities, answers.CityId)?.Id;
    }

    private void GoBack() => Nav.NavigateTo(JobPostRoutes.Mine);

    /// <summary>Another trade has other services: the picked service is cleared.</summary>
    private void PickTrade(string? categoryId)
    {
        if (_draft.CategoryId != categoryId)
        {
            _draft.CategoryId = categoryId;
            _draft.ServiceId = null;
        }
    }

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
