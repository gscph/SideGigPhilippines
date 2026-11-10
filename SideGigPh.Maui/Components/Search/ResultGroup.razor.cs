using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Components.Search;

/// <summary>A titled block of results ("In Taytay (3)"), best match first. Renders nothing when empty.</summary>
public partial class ResultGroup : ComponentBase
{
    [Parameter, EditorRequired] public string Title { get; set; } = string.Empty;

    [Parameter, EditorRequired] public IReadOnlyList<SearchHit> Hits { get; set; } = [];
}
