using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using SideGigPh.Contracts;
using SideGigPh.Maui.Components.Professional;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Onboarding;

/// <summary>
/// Professional (Skilled Worker) interview: trades → jobs → towns → experience → rate →
/// preview. "Others" after the trades lets a worker type a kind of work we don't list: it is saved as their trade and
/// job (the jobs question is skipped), sent for analytics and matched with households who typed something similar, so
/// nobody is stuck on the trade question. The rate is their own range in two peso boxes ("From" / "Up to"); the guide's
/// points (minimum, average, above average, each tile showing its prices for their jobs: <see cref="Pricing.RateChoices"/>)
/// fill the boxes in one tap, and "depends on the job" moves on. The answers build the listing card the pro sees at the end; its name and number come
/// from the account (<see cref="ListingMapper.TakeAccount"/>), so they are never asked here.
/// One URL per step; answers are saved whenever the user moves forward. The rules (which step, what an
/// answer clears, where Back goes) are in <see cref="OnboardingSteps"/>.
/// </summary>
public partial class ProfessionalInterview : ComponentBase
{
    [Inject] private IOnboardingStore Store { get; set; } = default!;
    [Inject] private IIdentityProvider Identity { get; set; } = default!;
    [Inject] private LocalAccount Account { get; set; } = default!;
    [Inject] private ProWorkspace Workspace { get; set; } = default!;
    [Inject] private BusyIndicator Busy { get; set; } = default!;
    [Inject] private OtherJobNotes Notes { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;
    [Inject] private TimeProvider Clock { get; set; } = default!;
    [Inject] private ILogger<ProfessionalInterview> Logger { get; set; } = default!;

    /// <summary>
    /// Opens the first question still unanswered: a step past it is never shown, so asking for the last step
    /// lands there (or on the preview when everything is answered). Publish sends incomplete listings here.
    /// </summary>
    public static string FirstUnansweredPath => OnboardingSteps.ProFirstUnansweredPath;

    [Parameter] public int? Step { get; set; }

    private OnboardingState? _state;
    private ProStep _step = ProStep.Skills;
    private bool _finishing;

    // The "Others" text already sent, so going back and forward doesn't send it twice.
    private string? _notedOtherJob;

    // "Others" after the trades: the tile is picked, waiting for what they type and Next.
    private bool _pickedOtherTrade;
    private string? _otherTradeText;

    // The rate's two boxes, saved as their range on Next, and the guide tile that last filled them.
    private int? _rateFrom;
    private int? _rateUpTo;
    private string? _rateLevel;

    private bool CanSaveRate => RateRange.IsValid(_rateFrom, _rateUpTo);

    private EventCallback OtherTradeNext => _pickedOtherTrade ? EventCallback.Factory.Create(this, PickOtherTradeAsync) : default;

    private ProfessionalDraft Draft => _state!.Professional;

    // One bar with the setup after it (OnboardingSteps.ProgressOf).
    private double Progress => OnboardingSteps.ProgressOf(_step);

    protected override async Task OnInitializedAsync()
    {
        _state = await Store.LoadAsync();
        _notedOtherJob = Draft.OtherJobText?.Trim();

        // The preview card shows the account's name and number.
        Draft.TakeAccount(await Identity.GetCurrentUserAsync());
    }

    protected override void OnParametersSet()
    {
        if (_state is null)
        {
            return;
        }

        _step = Draft.StepToShow(Step);
        if (_step == ProStep.Towns)
        {
            Draft.PreselectDefaultTown();  // the launch's town starts ticked; the others stay tickable
        }
        _pickedOtherTrade = Draft.IsOtherTrade();
        _otherTradeText = _pickedOtherTrade ? Draft.OtherJobText : null;
        (_rateFrom, _rateUpTo) = Pricing.RateStart(Draft.RateTierId, Draft.ServiceIds);
        _rateLevel = Draft.RateTierId == ReferenceData.Quote ? ReferenceData.Quote : null;
    }

    private Task PickSkillAsync(string categoryId)
    {
        _pickedOtherTrade = categoryId == ReferenceData.OtherTradeId;
        if (_pickedOtherTrade)
        {
            return Task.CompletedTask;  // waits for what they type, then Next
        }

        Draft.PickTrade(categoryId);
        return AdvanceAsync();
    }

    private void SetOtherTradeText(string text) => _otherTradeText = text;

    /// <summary>"Others" with what they typed: saved as their trade and job, and the interview goes on to the towns.</summary>
    private Task PickOtherTradeAsync()
    {
        var typed = _otherTradeText?.Trim();
        if (typed != _notedOtherJob)
        {
            _notedOtherJob = typed;
            _ = Notes.NoteAsync(ReferenceData.OtherTradeId, UserRole.Professional, typed);  // never holds up the interview
        }

        Draft.PickTrade(ReferenceData.OtherTradeId);
        Draft.OtherJobText = typed;
        return AdvanceAsync();
    }

    private void ToggleService(string serviceId) => Draft.ToggleService(serviceId);

    private void ToggleAllJobs() => Draft.ToggleAllJobs();

    private void ToggleCity(string cityId) => Draft.ToggleTown(cityId);

    private void ToggleAllTowns() => Draft.ToggleAllTowns();

    private Task PickExperienceAsync(string id)
    {
        Draft.ExperienceId = id;
        return AdvanceAsync();
    }

    /// <summary>A guide point fills the two boxes with its prices for their jobs; "depends on the job" moves on.</summary>
    private Task PickRateAsync(string id)
    {
        if (id == ReferenceData.Quote)
        {
            Draft.RateTierId = id;
            return AdvanceAsync();
        }

        _rateLevel = id;
        if (Pricing.RateRange(id, Draft.ServiceIds) is { } range)
        {
            (_rateFrom, _rateUpTo) = range;
        }

        return Task.CompletedTask;  // they can change the amounts, then Next
    }

    private Task SaveRateAsync()
    {
        Draft.RateTierId = RateRange.Format(_rateFrom!.Value, _rateUpTo!.Value);
        return AdvanceAsync();
    }

    private void SetOtherJobText(string text) => Draft.OtherJobText = text;

    private async Task AdvanceAsync()
    {
        if (_step == ProStep.Services && Draft.OtherJobText?.Trim() is { Length: > 0 } typed && typed != _notedOtherJob)
        {
            _notedOtherJob = typed;
            _ = Notes.NoteAsync(Draft.CategoryId, UserRole.Professional, typed);  // never holds up the interview
        }

        var state = _state!;
        var path = Draft.NextPath(_step);
        if (state.CompletedAt is null)
        {
            state.ResumePath = path;
        }

        await Store.SaveAsync(state);
        Nav.NavigateTo(path);
    }

    /// <summary>
    /// The pro tapped Done: the brand splash (<see cref="BusyIndicator"/>, the same one as at launch) stays up while the
    /// answers and profile are saved and their workspace is loaded behind it, then the setup opens (photo,
    /// references, Publish: <see cref="OnboardingSteps.AfterInterviewPath"/>), or Pro home once live. It waits at most
    /// <see cref="OnboardingFinish.WaitLimit"/>, and not when a save failed: the next page loads its own data anyway.
    /// </summary>
    private async Task FinishAsync()
    {
        if (_finishing)
        {
            return;  // a second tap on Done
        }

        _finishing = true;
        await Busy.RunAsync(() => OnboardingFinish.WaitAsync(SaveAndLoadHomeAsync(), Logger, Clock));
        Nav.NavigateTo(Draft.AfterInterviewPath(), replace: true);
    }

    private async Task SaveAndLoadHomeAsync()
    {
        var state = _state!;
        state.CompletedAt ??= DateTimeOffset.Now;
        state.ResumePath = null;
        await Store.SaveAsync(state);
        await Account.SaveProfileAsync(); // Their side.
        await Workspace.EnsureDraftLoadedAsync(); // The setup comes next; Pro home loads chats and reviews itself.
        await Workspace.ListingEditedAsync(); // Edit listing on a live listing: customers see the new answers.
    }

    private void GoBack() => Nav.NavigateTo(Draft.BackPath(_step, finished: _state!.CompletedAt is not null));

    private static IReadOnlyCollection<string> Selected(string? id) =>
        id is null ? Array.Empty<string>() : new[] { id };
}
