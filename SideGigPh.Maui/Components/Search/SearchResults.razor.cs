using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Search;

/// <summary>
/// The Seeker's payoff: pros for their job and town, budget fits first, with Call and a
/// pre-filled Text built from their interview answers. If nobody serves their town yet, it
/// widens to every town and says so, instead of showing an empty screen.
/// </summary>
public partial class SearchResults : ComponentBase
{
    [Inject] private IOnboardingStore Store { get; set; } = default!;
    [Inject] private IProfessionalProfileRepository Profiles { get; set; } = default!;
    [Inject] private IContactLauncher Phone { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    private SeekerAnswers _answers = new();
    private IReadOnlyList<ProfessionalListing> _results = [];
    private IReadOnlyList<AnswerChip> _chips = [];
    private bool _loading = true;
    private string? _widenedFromTown;
    private string? _error;

    private string Title => _results.Count == 1
        ? T.Get(Copy.ResultsTitleOne)
        : T.Format(Copy.ResultsTitle, _results.Count);

    protected override async Task OnInitializedAsync()
    {
        var state = await Store.LoadAsync();
        if (state.Seeker.CategoryId is not { } categoryId)
        {
            Nav.NavigateTo("/", replace: true);
            return;
        }

        _answers = state.Seeker;
        _chips = BuildChips();

        var results = await Profiles.SearchAsync(new ProfessionalSearchQuery(categoryId, _answers.CityId));
        if (results.Count == 0 && _answers.CityId is not null)
        {
            results = await Profiles.SearchAsync(new ProfessionalSearchQuery(categoryId, null));
            if (results.Count > 0)
            {
                _widenedFromTown = Label(ReferenceData.Find(ReferenceData.Cities, _answers.CityId));
            }
        }

        _results = results
            .OrderByDescending(FitsBudget)
            .ThenByDescending(pro => _answers.ServiceId is not null && pro.ServiceIds.Contains(_answers.ServiceId))
            .ToList();
        _loading = false;
    }

    /// <summary>The pro's starting price tier is at or below the Seeker's budget tier.</summary>
    private bool FitsBudget(ProfessionalListing pro)
    {
        var budget = ReferenceData.PriceRank(_answers.BudgetId);
        var rate = ReferenceData.PriceRank(pro.RateTierId);
        return budget >= 0 && rate >= 0 && rate <= budget;
    }

    private async Task CallAsync(ProfessionalListing pro) =>
        _error = await Phone.CallAsync(pro.MobileNumber) ? null : T.Get(Copy.ContactFailed);

    private async Task TextAsync(ProfessionalListing pro) =>
        _error = await Phone.TextAsync(pro.MobileNumber, BuildMessage()) ? null : T.Get(Copy.ContactFailed);

    private void EditStep(int step) => Nav.NavigateTo($"/start/seeker/{step}");

    private string BuildMessage()
    {
        var job = Label(ReferenceData.FindService(_answers.ServiceId)
            ?? ReferenceData.Find(ReferenceData.Categories, _answers.CategoryId));
        var town = Label(ReferenceData.Find(ReferenceData.Cities, _answers.CityId));
        var template = _answers.UrgencyId switch
        {
            ReferenceData.UrgencyToday => Copy.MessageToday,
            ReferenceData.UrgencyThisWeek => Copy.MessageThisWeek,
            _ => Copy.MessageCanvass,
        };
        return T.Format(template, job, town);
    }

    // Step numbers match SeekerInterview's steps.
    private IReadOnlyList<AnswerChip> BuildChips() =>
    [
        new(Label(ReferenceData.Find(ReferenceData.Categories, _answers.CategoryId)), 1),
        new(Label(ReferenceData.FindService(_answers.ServiceId)), 2),
        new(Label(ReferenceData.Find(ReferenceData.Cities, _answers.CityId)), 3),
        new(Label(ReferenceData.Find(ReferenceData.Urgencies, _answers.UrgencyId)), 4),
        new(Label(ReferenceData.Find(ReferenceData.BudgetOptions, _answers.BudgetId)), 5),
    ];

    private string Label(CatalogOption? option) => option is null ? string.Empty : T.Get(option.Label);
}
