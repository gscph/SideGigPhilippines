using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Onboarding;

/// <summary>
/// Shown under a trade's jobs once "Others / Wala sa nabanggit" is picked: the person may type the job. The text is
/// sent for analytics only (<see cref="OtherJobNotes.NoteAsync"/>); the job itself stays "Others". The interviews' trade
/// question uses it too, under the "Others" tile after the trades.
/// </summary>
public partial class OtherJobField : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    /// <summary>The question above the box, e.g. "What job do you need?".</summary>
    [Parameter, EditorRequired] public string Label { get; set; } = string.Empty;

    /// <summary>The line under the box; "Optional. It helps us add more jobs." when not given.</summary>
    [Parameter] public string? Help { get; set; }

    [Parameter] public string? Value { get; set; }

    [Parameter] public EventCallback<string> ValueChanged { get; set; }
}
