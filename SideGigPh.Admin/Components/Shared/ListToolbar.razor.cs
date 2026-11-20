using Microsoft.AspNetCore.Components;

namespace SideGigPh.Admin.Components.Shared;

/// <summary>
/// A list's toolbar (inside the grid's ToolBarContent): the search box (sent half a second after typing stops), the
/// list's one filter menu, and how many rows matched.
/// </summary>
public partial class ListToolbar
{
    [Parameter] public string? Search { get; set; }
    [Parameter] public EventCallback<string> SearchChanged { get; set; }
    [Parameter] public string SearchHint { get; set; } = "Search";

    [Parameter] public string Filter { get; set; } = "";
    [Parameter] public EventCallback<string> FilterChanged { get; set; }
    [Parameter] public string FilterLabel { get; set; } = "Show";
    [Parameter] public IReadOnlyList<FilterChoice> Filters { get; set; } = [];

    [Parameter] public int? Total { get; set; }
}
