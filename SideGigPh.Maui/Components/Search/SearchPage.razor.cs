using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Components.Shared;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Search;

/// <summary>
/// Search results (under the Home tab, opened from the home search box or a trade icon). The Seeker types what they need in their own words or taps a trade, narrows
/// to a job, and sets town and budget. Results come ranked by <c>SearchRanker</c>: pros who go
/// to their town first, then other towns, each best-fit first. Tapping a pro opens their profile.
/// Every change is saved, so the search is still here after Back or a restart.
/// </summary>
public partial class SearchPage : ComponentBase
{
    /// <summary>Chip id for "all of this trade's jobs".</summary>
    private const string AllJobs = "all";

    [Inject] private SeekerWorkspace Workspace { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;

    private SearchResult? _result;
    private string? _unmatchedText;
    private IReadOnlyList<string> _suggestedCategoryIds = [];
    private int _searchVersion;

    private SearchRequest Request => Workspace.Request;

    private string TownLabel => Label(ReferenceData.Find(ReferenceData.Cities, Request.CityId));

    /// <summary>
    /// The selected trade first, then other trades the typed words matched, then the rest. When the
    /// words didn't settle on one trade, the trades they matched come first and nothing is selected.
    /// </summary>
    private IReadOnlyList<ChoiceItem> CategoryItems
    {
        get
        {
            var selected = _unmatchedText is null ? Request.CategoryId : null;
            var first = new[] { selected }.Concat(_suggestedCategoryIds).OfType<string>().Distinct().ToList();
            var ordered = first
                .Select(id => ReferenceData.Find(ReferenceData.Categories, id))
                .OfType<CatalogOption>()
                .Concat(ReferenceData.Categories.Where(category => !first.Contains(category.Id)));
            return ChoiceItem.From(ordered, T);
        }
    }

    private IReadOnlyList<ChoiceItem> ServiceItems
    {
        get
        {
            var category = ReferenceData.Find(ReferenceData.Categories, Request.CategoryId);
            IReadOnlyList<CatalogOption> services = category is null ? [] : ReferenceData.ServicesByCategory[category.Id];
            return
            [
                new ChoiceItem(AllJobs, T.Format(Copy.SearchAllJobs, Label(category))),
                .. ChoiceItem.From(services, T),
            ];
        }
    }

    private IReadOnlyList<ChoiceItem> Towns => T.TownChoices();

    private IReadOnlyList<ChoiceItem> Budgets => ChoiceItem.From(ReferenceData.BudgetOptions, T);

    private string Summary => _result!.Count == 1
        ? T.Get(Copy.ResultsTitleOne)
        : T.Format(Copy.ResultsTitle, _result.Count);

    private string FallbackNote => T.Format(
        Copy.ResultsServiceFallback,
        Label(ReferenceData.FindService(Request.ServiceId)),
        Label(ReferenceData.Find(ReferenceData.Categories, Request.CategoryId)));

    protected override async Task OnInitializedAsync()
    {
        var intent = Workspace.CurrentIntent;
        _unmatchedText = Workspace.Answers.SearchText is { } words && !intent.Matched ? words : null;
        _suggestedCategoryIds = intent.OtherCategoryIds;
        await RunSearchAsync();
    }

    private async Task SearchForAsync(string words)
    {
        var intent = await Workspace.SearchForAsync(words);
        _unmatchedText = intent.Matched || string.IsNullOrWhiteSpace(words) ? null : words.Trim();
        _suggestedCategoryIds = intent.OtherCategoryIds;
        await RunSearchAsync();
    }

    private async Task PickCategoryAsync(string categoryId)
    {
        await Workspace.PickCategoryAsync(categoryId);
        _unmatchedText = null;
        _suggestedCategoryIds = [];
        await RunSearchAsync();
    }

    private async Task PickServiceAsync(string serviceId)
    {
        await Workspace.PickServiceAsync(serviceId == AllJobs ? null : serviceId);
        await RunSearchAsync();
    }

    private async Task PickTownAsync(string cityId)
    {
        await Workspace.PickTownAsync(cityId);
        await RunSearchAsync();
    }

    private async Task PickBudgetAsync(string budgetId)
    {
        await Workspace.PickBudgetAsync(budgetId);
        await RunSearchAsync();
    }

    /// <summary>Searches with the saved request; a slower earlier search never overwrites a newer one.</summary>
    private async Task RunSearchAsync()
    {
        var version = ++_searchVersion;
        var result = await Workspace.SearchAsync();
        if (version == _searchVersion)
        {
            _result = result;
        }
    }

    private string Label(CatalogOption? option) => option is null ? string.Empty : T.Get(option.Label);
}
