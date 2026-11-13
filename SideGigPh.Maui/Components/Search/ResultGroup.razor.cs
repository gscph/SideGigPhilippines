using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Components.Search;

/// <summary>
/// A titled block of results ("Goes to Taytay (7)"), best match first. Shows the results loaded so far; the title
/// counts all of them. Renders nothing when none are loaded yet.
/// </summary>
public partial class ResultGroup : ComponentBase
{
    [Parameter, EditorRequired] public string Title { get; set; } = string.Empty;

    /// <summary>The results shown so far.</summary>
    [Parameter, EditorRequired] public IReadOnlyList<SearchHit> Hits { get; set; } = [];

    /// <summary>How many results this group has in all, shown in the title.</summary>
    [Parameter, EditorRequired] public int Total { get; set; }
}
