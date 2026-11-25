using Microsoft.AspNetCore.Components;

namespace SideGigPh.Maui.Components.Shared;

/// <summary>A square work photo in the pro's gallery, with a remove button in the corner (hidden when a published listing would drop below 2 photos).</summary>
public partial class PhotoTile : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    [Parameter, EditorRequired] public string Url { get; set; } = string.Empty;

    [Parameter] public bool CanRemove { get; set; } = true;

    [Parameter] public EventCallback OnRemove { get; set; }
}
