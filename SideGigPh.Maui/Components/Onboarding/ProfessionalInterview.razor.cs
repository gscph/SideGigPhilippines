using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using SideGigPh.Maui.Components.Professional;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Onboarding;

/// <summary>
/// Professional (Skilled Worker) interview: trades → jobs → towns → experience → rate →
/// preview. "Others" after the trades lets a worker type a kind of work we don't list (sent for analytics), then thanks
/// them and goes back to the trades. The rate is a point of each job's price guide (minimum, average, above average,
/// each tile showing its prices for their jobs: <see cref="Pricing.RateChoices"/>) or "depends on the job". The answers build the listing card the pro sees at the end; its name and number come
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

    // "Others" after the trades: never saved as a trade, only typed, sent and thanked for.
    private bool _pickedOtherTrade;
    private string? _otherTradeText;
    private bool _otherTradeNoted;

    private EventCallback OtherTradeNext => _pickedOtherTrade ? EventCallback.Factory.Create(this, NoteOtherTrade) : default;

    private ProfessionalDraft Draft => _state!.Professional;

    // Step 1 already shows some progress (endowed progress); the preview is 100%.
    private double Progress => (int)_step / (double)ProStep.Preview;

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
        PickAnotherTrade();
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

    private void NoteOtherTrade()
    {
        _ = Notes.NoteAsync(ReferenceData.OtherTradeId, UserRole.Professional, _otherTradeText);  // never holds up the interview
        _otherTradeNoted = true;
    }

    private void PickAnotherTrade()
    {
        _pickedOtherTrade = false;
        _otherTradeText = null;
        _otherTradeNoted = false;
    }

    private void ToggleService(string serviceId) => Draft.ToggleService(serviceId);

    private void ToggleCity(string cityId) => Draft.ToggleTown(cityId);

    private void ToggleAllTowns() => Draft.ToggleAllTowns();

    private Task PickExperienceAsync(string id)
    {
        Draft.ExperienceId = id;
        return AdvanceAsync();
    }

    private Task PickRateAsync(string id)
    {
        Draft.RateTierId = id;
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
        var path = OnboardingSteps.PathOf(_step + 1);
        if (state.CompletedAt is null)
        {
            state.ResumePath = path;
        }

        await Store.SaveAsync(state);
        Nav.NavigateTo(path);
    }

    /// <summary>
    /// The pro tapped Done: the brand splash (<see cref="BusyIndicator"/>, the same one as at launch) stays up while the
    /// answers and profile are saved and their workspace is loaded behind it, then the setup opens (photo, work photos,
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

    private void GoBack() => Nav.NavigateTo(OnboardingSteps.BackPath(_step, finished: _state!.CompletedAt is not null));

    private static IReadOnlyCollection<string> Selected(string? id) =>
        id is null ? Array.Empty<string>() : new[] { id };
}
