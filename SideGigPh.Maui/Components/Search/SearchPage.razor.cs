using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Components.Seeker;
using SideGigPh.Maui.Components.Shared;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Search;

/// <summary>
/// Search results (under the Home tab, opened from the home search box or a trade icon). The Seeker types what they need in their own words or taps a trade, narrows
/// to a job, and sets town, budget, the order (best match or lowest price) and when they need it (which only words their first message: results stay the same). Results come ranked by the server, 4 at a time (more as they scroll): pros who go
/// to their town first, then other towns, each best-fit first. Tapping a pro opens their profile.
/// Every change is saved, so the search is still here after Back or a restart.
/// </summary>
public partial class SearchPage : ComponentBase
{
    [Inject] private SeekerWorkspace Workspace { get; set; } = default!;
    [Inject] private SignInGate Gate { get; set; } = default!;
    [Inject] private MobileAppTheme.Services.INetworkStatus Network { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    private SearchResult? _result;
    private IReadOnlyList<string> _suggestedCategoryIds = [];
    private int _searchVersion;

    private SearchRequest Request => Workspace.Request;

    private string TownLabel => Label(ReferenceData.Find(ReferenceData.Cities, Request.CityId));

    /// <summary>
    /// With nothing picked, every trade; after typed words that searched "Others", only the trades they came close to.
    /// </summary>
    private IReadOnlyList<ChoiceItem> CategoryItems => ChoiceItem.From(
        _suggestedCategoryIds.Count > 0 ? _suggestedCategoryIds.Select(ReferenceData.FindTrade).OfType<CatalogOption>() : ReferenceData.Categories, T);

    // Best match or lowest price first: being over the budget never hides a worker in either.
    private IReadOnlyList<ChoiceItem> Orders =>
    [
        new ChoiceItem(nameof(SearchOrder.BestMatch), T.Get(Copy.SortBestMatch)),
        new ChoiceItem(nameof(SearchOrder.LowestPrice), T.Get(Copy.SortLowestPrice)),
    ];

    private IReadOnlyList<ChoiceItem> Budgets => ChoiceItem.From(Pricing.BudgetChoices(Request.ServiceId, Request.CategoryId, Request.BudgetId), T);

    protected override async Task OnInitializedAsync()
    {
        // Seekers never have to add a phone number: a Google sign-in searches as it is.
        if (await Gate.RedirectAsync(SeekerRoutes.Search) is { } step)
        {
            Nav.NavigateTo(step, replace: true);
            return;
        }

        _suggestedCategoryIds = Workspace.SuggestedTrades;
        await RunSearchAsync();
    }

    private async Task PickCategoryAsync(string categoryId)
    {
        await Workspace.PickCategoryAsync(categoryId);
        _suggestedCategoryIds = [];
        await RunSearchAsync();
    }

    private async Task PickOrderAsync(string orderId)
    {
        await Workspace.PickOrderAsync(Enum.Parse<SearchOrder>(orderId));
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

    /// <summary>The Seeker scrolled to the end: the next page goes under the results (unless the search changed meanwhile).</summary>
    private async Task LoadMoreAsync()
    {
        if (_result is null)
        {
            return;
        }

        var version = _searchVersion;
        var start = _result;
        var more = await Workspace.MoreResultsAsync(start);
        if (version == _searchVersion && ReferenceEquals(start, _result))
        {
            _result = more;
        }
    }

    private string Label(CatalogOption? option) => option is null ? string.Empty : T.Get(option.Label);
}
