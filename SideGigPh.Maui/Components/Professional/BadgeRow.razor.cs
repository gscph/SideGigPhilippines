using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Components.Professional;

/// <summary>
/// One line of the pro's badges page: the badge's icon, its name, a gray note (what to send, or how it stands) and a
/// state chip. With <see cref="Href"/> the line opens the page that sends the proof; without it there is nothing to do.
/// </summary>
public partial class BadgeRow : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    [Parameter, EditorRequired] public string Icon { get; set; } = string.Empty;

    [Parameter, EditorRequired] public string Title { get; set; } = string.Empty;

    [Parameter, EditorRequired] public string Note { get; set; } = string.Empty;

    [Parameter] public BadgeState State { get; set; }

    /// <summary>Where a tap goes (send the proof), or null when nothing can be sent now.</summary>
    [Parameter] public string? Href { get; set; }
}
