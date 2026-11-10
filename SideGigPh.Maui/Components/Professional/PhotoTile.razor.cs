using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Professional;

/// <summary>A square work photo in the pro's gallery, with a remove button in the corner.</summary>
public partial class PhotoTile : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    [Parameter, EditorRequired] public string Url { get; set; } = string.Empty;

    [Parameter] public EventCallback OnRemove { get; set; }
}
