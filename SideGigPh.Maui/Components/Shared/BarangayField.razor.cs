using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Services;

namespace SideGigPh.Maui.Components.Shared;

/// <summary>
/// The optional "Barangay" choice for a town: the barangays we serve there (server data, <c>004-config.sql</c>) after
/// "Not sure / skip", which is where it starts. Shows nothing for a town without a list. It only reports the pick
/// (null for "Not sure"); the parent keeps it. Used by the household's town question and the post form.
/// </summary>
public partial class BarangayField : ComponentBase
{
    // The "Not sure / skip" item's value: MudSelect needs a non-null one. Reported to the parent as null.
    private const string Unsure = "";

    [Inject] private AppText T { get; set; } = default!;

    /// <summary>The town whose barangays are offered.</summary>
    [Parameter, EditorRequired] public string? TownId { get; set; }

    /// <summary>The picked barangay's id, or null for "Not sure".</summary>
    [Parameter] public string? Value { get; set; }

    [Parameter] public EventCallback<string?> ValueChanged { get; set; }

    [Parameter] public bool Disabled { get; set; }

    [Parameter] public string? Class { get; set; }

    private IReadOnlyList<CatalogOption> Barangays => ReferenceData.BarangaysOf(TownId);

    private Task PickAsync(string id) => ValueChanged.InvokeAsync(id == Unsure ? null : id);
}
