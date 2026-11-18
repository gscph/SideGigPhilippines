using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Components.Search;

/// <summary>
/// A block of results under a heading that says where ("9 skilled workers in Angono"), best match first, one
/// <see cref="ResultRow"/> each. Shows the results loaded so far; renders nothing when none are loaded yet.
/// </summary>
public partial class ResultGroup : ComponentBase
{
    [Parameter, EditorRequired] public string Title { get; set; } = string.Empty;

    /// <summary>The results shown so far.</summary>
    [Parameter, EditorRequired] public IReadOnlyList<SearchHit> Hits { get; set; } = [];

    /// <summary>The search, for each row's town and price.</summary>
    [Parameter, EditorRequired] public SearchRequest Request { get; set; } = default!;
}
