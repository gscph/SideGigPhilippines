using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using SideGigPh.Contracts;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.JobPosts;

/// <summary>
/// The form to post what a household needs done, full screen outside the tab shell, laid out like Airbnb's "Review and
/// continue": one card with What (kind of work + exact service, "Others" too), Where (town, the launch's default town
/// when they have none, optional barangay and "Use my location"), When (optional: a day on a calendar, workers see it,
/// <see cref="PostDay"/>) and Your budget (from–to, the suggested minimum price
/// as a guide only), each changed on its own full-screen sheet (Cancel / Save, <see cref="PostReview"/>) with its own URL
/// (<see cref="JobPostRoutes.Sheet"/>), so the phone's Back closes it like Cancel, or goes from a trade's jobs back to the
/// trades; then the post's own title (its header for both sides, the service shown under it), what is wrong and up to three photos. Each row still missing says what to do, the footer names everything
/// still needed, and Post stays tappable: a tap with something missing turns the notes red and opens the first one. It starts with the trade, service, town and barangay
/// from their last search or post, so most people only type the problem and a number. Post leads back to their posts.
/// The same form at <c>/posts/{id}/edit</c> changes an open post (<see cref="JobBoard.EditDraft"/>): filled in, "Save
/// changes", the trade and service fixed once offers came in, and a note instead when the post closed meanwhile.
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
    [Inject] private TimeProvider Clock { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;

    /// <summary>The post to change (<c>/posts/{id}/edit</c>), or null for a new post.</summary>
    [Parameter] public long? Id { get; set; }

    /// <summary>The open sheet's part of the URL (<see cref="PostReview.Slug"/>), or null on the form itself.</summary>
    [Parameter] public string? Sheet { get; set; }

    /// <summary>On the What sheet: the trade whose jobs are shown (its second page), or null for the trades.</summary>
    [Parameter] public string? Trade { get; set; }

    private JobPostDraft _draft = new();
    private bool _closed; // Edit post: it isn't theirs or no longer open
    private PhotoAddResult? _photoResult;
    private PostPart? _sheet;
    private PostSnapshot? _before;
    private bool _jobsStep; // the What sheet's second page: the picked trade's jobs
    private bool _saved; // Save was tapped: the answers stay when the form comes back
    private bool _leaving; // a sheet is going back to the form (waiting for the browser's history)
    private bool _tried; // Post was tapped with something missing: the notes turn red
    private MudTextField<string>? _title;
    private MudTextField<string>? _description;
    private bool _loaded; // the post (Edit) or the last search's answers are in the draft
    private bool _busy;
    private bool _failed;

    public override Task SetParametersAsync(ParameterView parameters)
    {
        // The same page stays on screen between the form and its sheets: a URL without a sheet must clear the old one.
        Sheet = null;
        Trade = null;
        return base.SetParametersAsync(parameters);
    }

    protected override async Task OnInitializedAsync()
    {
        await LoadAsync();
        _loaded = true; // the rows can't be changed before this: a sheet's Cancel would put back the empty draft
    }

    private async Task LoadAsync()
    {
        if (!await Workspace.EnsureLoadedAsync())
        {
            Nav.NavigateTo("/", replace: true);
            return;
        }

        if (Id is { } postId)
        {
            if (await Board.FindMineAsync(postId) is { IsOpen: true } post)
            {
                _draft = JobBoard.EditDraft(post);
            }
            else
            {
                _closed = true;
            }

            return;
        }

        if (Board.TakeUnsent() is { } kept)
        {
            _draft = kept; // closed with ✕ earlier: the problem and photos come back as typed
            return;
        }

        _draft = JobBoard.DraftFrom(Workspace.Answers, Today);
    }

    /// <summary>Today in the Philippines: the calendar's first day.</summary>
    private DateOnly Today => PostDay.Today(Clock);

    /// <summary>
    /// The URL changed between the form and a sheet (a tap here, Save, Cancel or the phone's Back). Arriving on the form
    /// without Save puts the answers back as they were when the sheet opened, so Back is the same as Cancel.
    /// </summary>
    protected override void OnParametersSet()
    {
        var part = _closed ? null : PostReview.FromSlug(Sheet);
        if (part is null)
        {
            if (_sheet is not null && !_saved && _before is not null)
            {
                PostReview.Restore(_draft, _before);
            }

            _before = null;
            _saved = false;
        }
        else
        {
            _before ??= PostReview.Take(_draft);
            if (part == PostPart.What && Trade is not null && _draft.CategoryId != Trade)
            {
                PostReview.PickTrade(_draft, Trade); // opened by its URL: the jobs shown are the picked trade's
            }
        }

        _sheet = part;
        _jobsStep = part == PostPart.What && PostReview.HasJobChoice(Trade);
        _leaving = false;
    }

    /// <summary>Changing an open post (<see cref="Id"/>), not posting a new one.</summary>
    private bool Editing => Id is not null;

    private void GoBack()
    {
        if (!Editing && _loaded)
        {
            Board.KeepUnsent(_draft); // an edit is dropped: the post itself still holds it; before loading, nothing to keep
        }

        Nav.NavigateTo(JobPostRoutes.Mine);
    }

    private void OpenSheet(PostPart part)
    {
        if (_busy || _leaving || !_loaded)
        {
            return;
        }

        _before = PostReview.Take(_draft);
        Nav.NavigateTo(JobPostRoutes.Sheet(Id, part));
    }

    /// <summary>Cancel or the close button: back to the form, which puts the answers back as they were.</summary>
    private Task CancelSheetAsync() => LeaveSheetAsync();

    private Task SaveSheetAsync()
    {
        if (_sheet is not { } part || !PostReview.CanSave(_draft, part))
        {
            return Task.CompletedTask;
        }

        _saved = true;
        return LeaveSheetAsync();
    }

    /// <summary>Back to the form through the browser's history, so the phone's Back after it leaves the form as before.</summary>
    private Task LeaveSheetAsync() => GoBackAsync(PostReview.SheetDepth(Trade));

    /// <summary>A kind of work was tapped: on to its jobs (a trade without a choice, "Others", stays here).</summary>
    private void TradePicked()
    {
        if (!_leaving && PostReview.HasJobChoice(_draft.CategoryId))
        {
            Nav.NavigateTo(JobPostRoutes.Sheet(Id, PostPart.What, _draft.CategoryId));
        }
    }

    /// <summary>← on the jobs page: back to the kinds of work, the picks kept (Cancel still puts everything back).</summary>
    private Task BackToTradesAsync() => GoBackAsync(1);

    private async Task GoBackAsync(int pages)
    {
        if (_leaving)
        {
            return; // a second tap before the history moved
        }

        _leaving = true;
        try
        {
            await JS.InvokeVoidAsync("history.go", -pages);
        }
        catch (JSException)
        {
            _leaving = false;
            Nav.NavigateTo(JobPostRoutes.Form(Id), replace: true);
        }
    }

    /// <summary>The jobs page is titled with its trade ("Construction"), every other sheet with its part.</summary>
    private string SheetTitle(PostPart part) =>
        _jobsStep && ReferenceData.FindTrade(_draft.CategoryId) is { } trade ? T.Get(trade.Label) : T.PostSheetTitle(part);

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

    private bool TitleMissing => !JobBoard.IsTitleOk(_draft.Title);

    private string TitleNote => JobBoard.MissingText(JobPostField.Title, T, _draft);

    private bool DescriptionMissing => !JobBoard.IsDescriptionOk(_draft.Description);

    private string DescriptionNote => JobBoard.MissingText(JobPostField.Description, T, _draft);

    private async Task PostAsync()
    {
        if (_busy)
        {
            return; // a second tap before the button re-rendered as disabled
        }

        if (JobBoard.FirstMissing(_draft, Today) is { } missing)
        {
            // Post stays tappable: it shows what is missing in red and opens the first one.
            _tried = true;
            if (PostReview.PartOf(missing) is { } part)
            {
                OpenSheet(part);
            }
            else if ((missing == JobPostField.Title ? _title : _description) is { } field)
            {
                await field.FocusAsync();
            }

            return;
        }

        _busy = true;
        _failed = false;
        try
        {
            if (Editing)
            {
                switch (await Busy.RunAsync(() => Board.SaveEditAsync(_draft)))
                {
                    case JobPostResult.Posted:
                        Nav.NavigateTo(JobPostRoutes.Mine, replace: true);
                        return;
                    case JobPostResult.Closed:
                        _closed = true;
                        return;
                    default:
                        _failed = true;
                        return;
                }
            }

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
