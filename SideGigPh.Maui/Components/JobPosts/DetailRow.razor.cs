using Microsoft.AspNetCore.Components;
using Color = MudBlazor.Color;

namespace SideGigPh.Maui.Components.JobPosts;

/// <summary>
/// One row of a request's card (Airbnb's "Dates / Jan 8 – 10 [Change]"): a bold label, the value under it, an optional
/// muted line, what is still missing (on the post form), extra content (photos) and an optional gray action button on the right.
/// </summary>
public partial class DetailRow : ComponentBase
{
    [Parameter, EditorRequired] public string Label { get; set; } = string.Empty;

    [Parameter] public string? Value { get; set; }

    /// <summary>The value's color: <see cref="Color.Success"/> for money.</summary>
    [Parameter] public Color ValueColor { get; set; } = Color.Default;

    /// <summary>A muted line under the value ("Waiting for the customer"), or null.</summary>
    [Parameter] public string? Detail { get; set; }

    /// <summary>What is still missing before the form can be sent ("Pick your town."), or null.</summary>
    [Parameter] public string? Missing { get; set; }

    /// <summary>The household already tapped Post: <see cref="Missing"/> shows in red instead of gray.</summary>
    [Parameter] public bool Alert { get; set; }

    /// <summary>The gray button on the right ("Change"); none when null.</summary>
    [Parameter] public string? ActionLabel { get; set; }

    [Parameter] public EventCallback OnAction { get; set; }

    /// <summary>The page is sending: the button waits.</summary>
    [Parameter] public bool Busy { get; set; }

    [Parameter] public RenderFragment? ChildContent { get; set; }
}
