using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using SideGigPh.Contracts;
using SideGigPh.Maui.Components.Seeker;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Onboarding;

/// <summary>
/// Seeker interview: what job → which town (their one home town) → when →
/// budget → results. "Others" after the trades lets someone type a kind of work we don't offer (sent for analytics),
/// then thanks them and goes back to the trades. The budget offers three ranges from the job's price guide
/// (<see cref="Pricing.BudgetChoices"/>), "Other amount" (two peso boxes) and "Not sure yet". Each step has its own
/// URL so Android back walks the steps; every answer is saved as soon as it's tapped.
/// Once finished, tapping an answer chip on the results reopens one step and returns there.
/// The rules (which step, what an answer clears, where Next and Back go) are in <see cref="OnboardingSteps"/>.
/// </summary>
public partial class SeekerInterview : ComponentBase
{
    [Inject] private IOnboardingStore Store { get; set; } = default!;
    [Inject] private LocalAccount Account { get; set; } = default!;
    [Inject] private OtherJobNotes Notes { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;
    [Inject] private BusyIndicator Busy { get; set; } = default!;
    [Inject] private TimeProvider Clock { get; set; } = default!;
    [Inject] private ILogger<SeekerInterview> Logger { get; set; } = default!;

    [Parameter] public int? Step { get; set; }

    private OnboardingState? _state;
    private SeekerStep _step = SeekerStep.Category;

    // The "Others" text already sent, so going back and forward doesn't send it twice.
    private string? _notedOtherJob;

    // "Others" after the trades: never saved as a trade, only typed, sent and thanked for.
    private bool _pickedOtherTrade;
    private string? _otherTradeText;
    private bool _otherTradeNoted;

    // "Other amount": the two boxes, saved as a range on Next.
    private bool _customBudget;
    private int? _customMin;
    private int? _customMax;

    // The last step started saving (it runs once, however often the page renders).
    private bool _finishing;

    private SeekerAnswers Answers => _state!.Seeker;

    private bool PickedOtherJob => ReferenceData.IsOtherJob(Answers.ServiceId);

    // Next shows only under "Others"; any other job moves on when tapped.
    private EventCallback OtherJobNext => PickedOtherJob ? EventCallback.Factory.Create(this, NextFromOtherJobAsync) : default;

    private bool HasOtherTradeText => !string.IsNullOrWhiteSpace(_otherTradeText);

    // Next shows only under "Others" (trades) and "Other amount" (budget); other tiles move on when tapped.
    private EventCallback OtherTradeNext => _pickedOtherTrade ? EventCallback.Factory.Create(this, NoteOtherTrade) : default;

    private EventCallback CustomBudgetNext => _customBudget ? EventCallback.Factory.Create(this, SaveCustomBudgetAsync) : default;

    private IReadOnlyList<CatalogOption> BudgetChoices =>
    [
        .. Pricing.BudgetChoices(Answers.ServiceId, Answers.CategoryId, Answers.BudgetId).Where(choice => choice.Id != ReferenceData.Unsure),
        new(Pricing.CustomBudget, Copy.BudgetCustom, AppImages.Custom, Copy.BudgetCustomHint),
        ReferenceData.UnsureOption,
    ];

    // "Most people pay ₱1,000 for Aircon cleaning." (the job's average), as an anchor above the choices.
    private string? BudgetWhy =>
        Pricing.GuideFor(Answers.ServiceId, Answers.CategoryId) is { } guide
            ? T.Format(Copy.BudgetTypical, Money.Peso(guide.Average), Label(ReferenceData.FindService(Answers.ServiceId)
                ?? ReferenceData.Find(ReferenceData.Categories, Answers.CategoryId)))
            : null;

    private bool IsEditing => _state!.CompletedAt is not null;

    // Step 1 already shows some progress (endowed progress); the loader step is 100%.
    private double Progress => (int)_step / (double)SeekerStep.Finding;

    protected override async Task OnInitializedAsync()
    {
        _state = await Store.LoadAsync();
        _notedOtherJob = Answers.OtherJobText?.Trim();
    }

    protected override void OnParametersSet()
    {
        if (_state is null)
        {
            return;
        }

        // Never show a step whose earlier answers are missing (e.g. a stale resume path).
        _step = Answers.StepToShow(Step);
        PickAnotherTrade();
        _customBudget = false;
    }

    private Task PickCategoryAsync(string id)
    {
        _pickedOtherTrade = id == ReferenceData.OtherTradeId;
        if (_pickedOtherTrade)
        {
            return Task.CompletedTask;  // waits for what they type, then Next
        }

        Answers.PickCategory(id);
        return AdvanceAsync();
    }

    private void SetOtherTradeText(string text) => _otherTradeText = text;

    private void NoteOtherTrade()
    {
        _ = Notes.NoteAsync(ReferenceData.OtherTradeId, UserRole.Seeker, _otherTradeText);  // never holds up the interview
        _otherTradeNoted = true;
    }

    private void PickAnotherTrade()
    {
        _pickedOtherTrade = false;
        _otherTradeText = null;
        _otherTradeNoted = false;
    }

    private Task PickServiceAsync(string id)
    {
        Answers.PickService(id);

        // "Others" waits for Next, so they can type the job first.
        return PickedOtherJob ? Store.SaveAsync(_state!) : AdvanceAsync();
    }

    private void SetOtherJobText(string text) => Answers.OtherJobText = text;

    private async Task NextFromOtherJobAsync()
    {
        if (Answers.OtherJobText?.Trim() is { Length: > 0 } typed && typed != _notedOtherJob)
        {
            _notedOtherJob = typed;
            _ = Notes.NoteAsync(Answers.CategoryId, UserRole.Seeker, typed);  // never holds up the interview
        }

        await AdvanceAsync();
    }

    private Task PickCityAsync(string id)
    {
        Answers.PickTown(id);
        return AdvanceAsync();
    }

    private Task PickWhenAsync(string id)
    {
        Answers.UrgencyId = id;
        return AdvanceAsync();
    }

    private Task PickBudgetAsync(string id)
    {
        _customBudget = id == Pricing.CustomBudget;
        if (_customBudget)
        {
            (_customMin, _customMax) = Pricing.CustomStart(Answers.BudgetId, Answers.ServiceId, Answers.CategoryId);
            return Task.CompletedTask;  // waits for the two amounts, then Next
        }

        Answers.BudgetId = id;
        return AdvanceAsync();
    }

    private Task SaveCustomBudgetAsync()
    {
        Answers.BudgetId = BudgetRange.Format(_customMin!.Value, _customMax!.Value);
        _customBudget = false;
        return AdvanceAsync();
    }

    private async Task AdvanceAsync()
    {
        var state = _state!;
        var path = Answers.NextPath(_step, IsEditing);
        if (!IsEditing)
        {
            state.ResumePath = path;
        }

        await Store.SaveAsync(state);
        Nav.NavigateTo(path);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_state is not null && _step == SeekerStep.Finding && !_finishing)
        {
            _finishing = true;
            await FinishAsync();
        }
    }

    /// <summary>
    /// The last step: the brand splash (<see cref="BusyIndicator"/>, the same one as at launch) stays up while the
    /// answers and profile are saved, at most <see cref="OnboardingFinish.WaitLimit"/>, then the results open.
    /// </summary>
    private async Task FinishAsync()
    {
        var state = _state!;
        state.CompletedAt = DateTimeOffset.Now;
        state.ResumePath = null;
        await Busy.RunAsync(() => OnboardingFinish.WaitAsync(SaveAsync(state), Logger, Clock));
        Nav.NavigateTo(SeekerRoutes.Search, replace: true);
    }

    private async Task SaveAsync(OnboardingState state)
    {
        await Store.SaveAsync(state);
        await Account.SaveProfileAsync();
    }

    private void GoBack() => Nav.NavigateTo(OnboardingSteps.BackPath(_step, IsEditing));

    private static IReadOnlyCollection<string> Selected(string? id) =>
        id is null ? Array.Empty<string>() : new[] { id };

    private string Label(CatalogOption? option) => option is null ? string.Empty : T.Get(option.Label);
}
