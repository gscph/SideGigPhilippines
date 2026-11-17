using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using SideGigPh.Contracts;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Onboarding;

/// <summary>
/// Seeker interview: what job → which town (their one home town) → when →
/// budget → search or post → the results, or the form to post what they need (pre-filled from these answers). "Others" after the trades lets someone type a kind of work we don't list: it is saved as their
/// trade and job (the job question is skipped), sent for analytics and matched with workers who typed something similar,
/// so nobody is stuck on the trade question. The budget is two peso boxes, "From" and "Up to"; the job's price guide
/// (<see cref="Pricing.BudgetChoices"/>) shows as ranges that fill them in one tap (a guide, not a rule), and "Not sure
/// yet" moves on. Each step has its own URL so Android back walks the steps; every answer is saved as soon as it's tapped.
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

    // "Others" after the trades: the tile is picked, waiting for what they type and Next.
    private bool _pickedOtherTrade;
    private string? _otherTradeText;

    // The budget's two boxes, saved as a range on Next.
    private int? _budgetMin;
    private int? _budgetMax;

    // The last step started saving (it runs once, however often the page renders).
    private bool _finishing;

    private SeekerAnswers Answers => _state!.Seeker;

    private bool PickedOtherJob => ReferenceData.IsOtherJob(Answers.ServiceId);

    // Next shows only under "Others"; any other job moves on when tapped.
    private EventCallback OtherJobNext => PickedOtherJob ? EventCallback.Factory.Create(this, NextFromOtherJobAsync) : default;

    private bool HasOtherTradeText => !string.IsNullOrWhiteSpace(_otherTradeText);

    // Next shows only under "Others" (trades); other trade tiles move on when tapped.
    private EventCallback OtherTradeNext => _pickedOtherTrade ? EventCallback.Factory.Create(this, PickOtherTradeAsync) : default;

    // The guide's ranges (and a range typed earlier), then "Not sure yet".
    private IReadOnlyList<CatalogOption> BudgetChoices => Pricing.BudgetChoices(Answers.ServiceId, Answers.CategoryId, Answers.BudgetId);

    // The tile the two boxes match, if any.
    private string? PickedBudget => _budgetMin is { } min ? BudgetRange.Format(min, _budgetMax) : null;

    // "Most people pay ₱1,000 for Aircon cleaning." (the job's average), as an anchor above the choices.
    private string? BudgetWhy =>
        Pricing.GuideFor(Answers.ServiceId, Answers.CategoryId) is { } guide
            ? T.Format(Copy.BudgetTypical, Money.Peso(guide.Average), Label(ReferenceData.FindService(Answers.ServiceId)
                ?? ReferenceData.FindTrade(Answers.CategoryId)))
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
        _pickedOtherTrade = Answers.IsOtherTrade();
        _otherTradeText = _pickedOtherTrade ? Answers.OtherJobText : null;
        (_budgetMin, _budgetMax) = Pricing.CustomStart(Answers.BudgetId, Answers.ServiceId, Answers.CategoryId);
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

    /// <summary>"Others" with what they typed: saved as their trade and job, and the interview goes on to the town.</summary>
    private Task PickOtherTradeAsync()
    {
        var typed = _otherTradeText?.Trim();
        if (typed != _notedOtherJob)
        {
            _notedOtherJob = typed;
            _ = Notes.NoteAsync(ReferenceData.OtherTradeId, UserRole.Seeker, typed);  // never holds up the interview
        }

        Answers.PickCategory(ReferenceData.OtherTradeId);
        Answers.OtherJobText = typed;
        return AdvanceAsync();
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

    /// <summary>A guide range fills the two boxes (an open-ended one leaves "Up to" for them); "Not sure yet" moves on.</summary>
    private Task PickBudgetAsync(string id)
    {
        if (BudgetRange.TryParse(id, out var min, out var max))
        {
            (_budgetMin, _budgetMax) = (min, max);
            return Task.CompletedTask;  // they can change the amounts, then Next
        }

        Answers.BudgetId = id;
        return AdvanceAsync();
    }

    private Task SaveBudgetAsync()
    {
        Answers.BudgetId = BudgetRange.Format(_budgetMin!.Value, _budgetMax!.Value);
        return AdvanceAsync();
    }

    private Task PickFindByAsync(string id)
    {
        Answers.FindBy = id;
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
    /// answers and profile are saved, at most <see cref="OnboardingFinish.WaitLimit"/>, then the results or the post form
    /// open (<see cref="OnboardingSteps.FinishPath"/>).
    /// </summary>
    private async Task FinishAsync()
    {
        var state = _state!;
        state.CompletedAt = DateTimeOffset.Now;
        state.ResumePath = null;
        await Busy.RunAsync(() => OnboardingFinish.WaitAsync(SaveAsync(state), Logger, Clock));
        Nav.NavigateTo(Answers.FinishPath(), replace: true);
    }

    private async Task SaveAsync(OnboardingState state)
    {
        await Store.SaveAsync(state);
        await Account.SaveProfileAsync();
    }

    private void GoBack() => Nav.NavigateTo(Answers.BackPath(_step, IsEditing));

    private static IReadOnlyCollection<string> Selected(string? id) =>
        id is null ? Array.Empty<string>() : new[] { id };

    private string Label(CatalogOption? option) => option is null ? string.Empty : T.Get(option.Label);
}
