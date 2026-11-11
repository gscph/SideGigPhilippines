using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Onboarding;

/// <summary>
/// The towns of one area (picked on the step before) as stacked answer tiles, with a filter box
/// when the area has many towns (Metro Manila). A Seeker taps their one home town; a Skilled Worker
/// ticks several, or the whole area with "Select all". It only reports taps; the parent decides.
/// </summary>
public partial class TownPicker : ComponentBase
{
    /// <summary>More towns than this and a filter box appears above them.</summary>
    private const int FilterFromTownCount = 10;

    [Inject] private AppText T { get; set; } = default!;

    /// <summary>The area (<see cref="ReferenceData.Areas"/> id) whose towns are shown.</summary>
    [Parameter, EditorRequired] public string AreaId { get; set; } = string.Empty;

    [Parameter] public IReadOnlyCollection<string> SelectedIds { get; set; } = [];

    /// <summary>Several towns may be ticked; shows "Select all" / "Remove all".</summary>
    [Parameter] public bool Multiple { get; set; }

    /// <summary>A town was tapped.</summary>
    [Parameter] public EventCallback<string> OnPick { get; set; }

    /// <summary>"Select all" / "Remove all" was tapped.</summary>
    [Parameter] public EventCallback OnPickAll { get; set; }

    private string? _filter;

    private CatalogOption Area => ReferenceData.Find(ReferenceData.Areas, AreaId)!;

    private IReadOnlyList<CatalogOption> Towns => ReferenceData.CitiesByArea[AreaId];

    private bool ShowFilter => Towns.Count > FilterFromTownCount;

    private bool AllSelected => Towns.All(town => SelectedIds.Contains(town.Id));

    private IReadOnlyList<ChoiceItem> Tiles =>
        ChoiceItem.From(Towns.Where(town => TownText.MatchesTown(_filter, T.Get(town.Label))), T);
}
