using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using SideGigPh.Contracts;
using SideGigPh.Maui.Components.Professional;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Onboarding;

/// <summary>
/// Professional (Skilled Worker) interview: trades → jobs → rate → towns → experience → selfie → number → preview,
/// then Publish. The selfie (<see cref="SelfieField"/>) is the proof a listing needs; the number question
/// (<see cref="NumberField"/>) adds an SMS-verified number to a Google or Apple account and is skipped when the account
/// already has one or today's codes are used up (<see cref="OnboardingSteps.AsksNumber"/>); its "Do this later" moves on
/// and Publish asks again. "Others" after the trades lets a worker type a kind of work we don't list: it is saved as their trade and
/// job (the jobs question is skipped), sent for analytics and matched with households who typed something similar, so
/// nobody is stuck on the trade question. The price is one peso box, their average price per service
/// (<see cref="RatePriceField"/>: only a guide for customers, with what others charge for their jobs). The answers
/// build the listing card the pro sees at the end; its name and number come
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
    [Inject] private SmsSendLimit SmsLimit { get; set; } = default!;
    [Inject] private ILogger<ProfessionalInterview> Logger { get; set; } = default!;

    [Parameter] public int? Step { get; set; }

    private OnboardingState? _state;
    private ProStep _step = ProStep.Skills;
    private bool _finishing;

    // Whether the number question is asked (OnboardingSteps.AsksNumber), and whether its code box is showing.
    private bool _asksNumber;
    private bool _codeSent;
    private NumberField? _number;

    // The "Others" text already sent, so going back and forward doesn't send it twice.
    private string? _notedOtherJob;

    // "Others" after the trades: the tile is picked, waiting for what they type and Next.
    private bool _pickedOtherTrade;
    private string? _otherTradeText;

    // The price box, saved on Next.
    private int? _rate;

    private bool CanSaveRate => WorkerPrice.IsValid(_rate);

    // Next only for a photo they already had; a new one moves on by itself.
    private EventCallback PhotoNext => Draft.HasProof ? EventCallback.Factory.Create(this, AdvanceAsync) : default;

    private EventCallback OtherTradeNext => _pickedOtherTrade ? EventCallback.Factory.Create(this, PickOtherTradeAsync) : default;

    private ProfessionalDraft Draft => _state!.Professional;

    // One bar with the setup after it (OnboardingSteps.ProgressOf).
    private double Progress => OnboardingSteps.ProgressOf(_step);

    protected override async Task OnInitializedAsync()
    {
        _state = await Store.LoadAsync();
        _notedOtherJob = Draft.OtherJobText?.Trim();

        // The selfie question saves through the workspace, which holds these same answers; it also puts the account's
        // name and number on them for the preview card.
        await Workspace.LoadInterviewDraftAsync();
        _asksNumber = AsksNumber();
    }

    private bool AsksNumber() =>
        OnboardingSteps.AsksNumber(Identity.Methods != AuthMethods.None, Workspace.HasVerifiedPhone, SmsLimit.WaitLeft());

    protected override void OnParametersSet()
    {
        if (_state is null)
        {
            return;
        }

        _step = Draft.StepToShow(Step, _asksNumber);
        if (OnboardingSteps.CorrectedPath(Step, _step) is { } corrected)
        {
            Nav.NavigateTo(corrected, replace: true);
        }

        _codeSent = false;
        if (_step == ProStep.Towns)
        {
            Draft.PreselectDefaultTown();  // the launch's town starts ticked; the others stay tickable
        }
        _pickedOtherTrade = Draft.IsOtherTrade();
        _otherTradeText = _pickedOtherTrade ? Draft.OtherJobText : null;
        _rate = Draft.RatePesos;
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

    private Task SaveRateAsync()
    {
        Draft.RatePesos = _rate;
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
        var path = Draft.NextPath(_step, _asksNumber);
        if (!state.HasFinished(UserRole.Professional))
        {
            state.ResumePath = path;
        }

        await Store.SaveAsync(state);
        Nav.NavigateTo(path);
    }

    /// <summary>
    /// The pro tapped Done: the brand splash (<see cref="BusyIndicator"/>, the same one as at launch) stays up while the
    /// answers and profile are saved and their workspace is loaded behind it, then the setup opens (the selfie,
    /// then Publish: <see cref="OnboardingSteps.AfterInterviewPath"/>), or Pro home once live. It waits at most
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
        state.Role = UserRole.Professional;
        state.ProFinishedAt ??= DateTimeOffset.Now;
        state.ResumePath = null;
        await Store.SaveAsync(state);
        await Account.SaveProfileAsync(); // Their side.
        await Workspace.EnsureDraftLoadedAsync(); // The setup comes next; Pro home loads chats and reviews itself.
        await Workspace.ListingEditedAsync(); // Edit listing on a live listing: customers see the new answers.
    }

    private void GoBack() => Nav.NavigateTo(SideSwitch.BackFrom(_state!, UserRole.Professional,
        Draft.BackPath(_step, finished: _state!.HasFinished(UserRole.Professional), _asksNumber)));

    private void CodeSent(string? number) => _codeSent = number is not null;

    // Back on the code box returns to the number box; on the number box, to the selfie.
    private async Task NumberBackAsync()
    {
        if (_number is null || !await _number.BackAsync())
        {
            GoBack();
        }
    }

    // The number is on the account now: the preview card shows it, and Back from the preview skips the question.
    private async Task NumberLinkedAsync()
    {
        Draft.TakeAccount(await Identity.GetCurrentUserAsync());
        _asksNumber = false;
        await AdvanceAsync();
    }

    private static IReadOnlyCollection<string> Selected(string? id) =>
        id is null ? Array.Empty<string>() : new[] { id };
}
