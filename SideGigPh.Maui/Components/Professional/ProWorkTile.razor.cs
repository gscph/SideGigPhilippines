using Microsoft.AspNetCore.Components;

namespace SideGigPh.Maui.Components.Professional;

/// <summary>
/// One of the two big tiles at the top of Pro home for the pro's daily work: Job Requests (filled, the main one) and
/// Inbox (outlined). A count chip shows what waits there (new requests, unread chats); the whole tile opens the page.
/// </summary>
public partial class ProWorkTile : ComponentBase
{
    [Parameter, EditorRequired] public string Href { get; set; } = string.Empty;

    [Parameter, EditorRequired] public string Icon { get; set; } = string.Empty;

    [Parameter, EditorRequired] public string Label { get; set; } = string.Empty;

    /// <summary>One line under the label: what waits there.</summary>
    [Parameter] public string? Detail { get; set; }

    /// <summary>How many wait there; no chip at 0.</summary>
    [Parameter] public int Count { get; set; }

    /// <summary>Another page follows the counted ones: the chip reads "20+".</summary>
    [Parameter] public bool CountHasMore { get; set; }

    /// <summary>Filled in the brand colour (the main tile) instead of outlined.</summary>
    [Parameter] public bool Primary { get; set; }

    private string CountLabel => CountHasMore ? $"{Count}+" : Count.ToString();

    private const string TileClass = "app-rounded pa-4 flex-1";

    private string? TileStyle => Primary ? "background: var(--mud-palette-primary);" : null;

    private string TextStyle => Primary ? "color: var(--mud-palette-primary-text);" : string.Empty;

    private string DetailStyle => Primary
        ? "color: var(--mud-palette-primary-text); opacity: .9;"
        : "color: var(--mud-palette-text-secondary);";

    private string IconStyle => Primary ? "color: var(--mud-palette-primary-text);" : "color: var(--mud-palette-primary);";
}
