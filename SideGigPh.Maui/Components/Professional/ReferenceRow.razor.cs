using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Professional;

/// <summary>One reference on the pro's list: name, number, and a remove button when one can be removed.</summary>
public partial class ReferenceRow : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    [Parameter, EditorRequired] public WorkReference Reference { get; set; } = default!;

    [Parameter] public bool CanRemove { get; set; }

    [Parameter] public EventCallback OnRemove { get; set; }
}
