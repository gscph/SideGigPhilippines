using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Seeker;

/// <summary>
/// The full-screen search sheet (<c>/seeker/find</c>), like Airbnb's: What? (words or a trade tile), Where? (the town,
/// the launch town pre-picked) and When?, one open at a time (<see cref="SearchSheet"/>), with "Clear all" and Search
/// pinned at the bottom. Opened from Seeker home's pill and the results' summary pill; Close goes back without
/// changing the search, Search saves the answers and opens the results.
/// </summary>
public partial class SearchSheetPage : ComponentBase
{
    [Inject] private SeekerWorkspace Workspace { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    /// <summary>Where Close goes: Seeker home or the results.</summary>
    [SupplyParameterFromQuery(Name = "back")] public string? Back { get; set; }

    private SearchSheet? _sheet;
    private bool _busy;

    private IReadOnlyList<ChoiceItem> Categories => ChoiceItem.From(ReferenceData.Categories, T);

    private IReadOnlyList<ChoiceItem> Towns => T.TownChoices();

    private IReadOnlyList<ChoiceItem> Urgencies => ChoiceItem.From(ReferenceData.Urgencies, T);

    protected override async Task OnInitializedAsync()
    {
        if (!await Workspace.EnsureLoadedAsync())
        {
            Nav.NavigateTo("/", replace: true);
            return;
        }

        _sheet = SearchSheet.From(Workspace.Answers);
    }

    private static IReadOnlyCollection<string> Selected(string? id) => id is null ? [] : [id];

    private void Close() => Nav.NavigateTo(SeekerRoutes.BackOrHome(Back));

    private async Task SearchAsync()
    {
        if (_busy || _sheet is not { CanSearch: true } sheet)
        {
            return;
        }

        _busy = true;
        try
        {
            if (sheet.Words is { } words)
            {
                await Workspace.SearchForAsync(words);
            }
            else if (sheet.NeedsTradePick(Workspace.Answers))
            {
                await Workspace.PickCategoryAsync(sheet.CategoryId!);
            }

            if (sheet.TownId is { } townId)
            {
                await Workspace.PickTownAsync(townId);
            }

            if (sheet.UrgencyId is { } urgencyId)
            {
                await Workspace.PickUrgencyAsync(urgencyId);
            }

            Nav.NavigateTo(SeekerRoutes.Search);
        }
        finally
        {
            _busy = false;
        }
    }
}
