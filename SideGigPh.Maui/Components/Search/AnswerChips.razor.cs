using Microsoft.AspNetCore.Components;

namespace SideGigPh.Maui.Components.Search;

/// <summary>The Seeker's interview answers echoed back as tappable chips.</summary>
public partial class AnswerChips : ComponentBase
{
    [Parameter, EditorRequired] public IReadOnlyList<AnswerChip> Items { get; set; } = [];

    /// <summary>Raised with the interview step to reopen.</summary>
    [Parameter] public EventCallback<int> OnEdit { get; set; }
}
