using Microsoft.AspNetCore.Components;
using MobileAppTheme.Localization;

namespace MobileAppTheme.Components.Shared;

/// <summary>
/// A destructive action (Start over, Delete account) with a second tap to confirm, since it can't be undone.
/// Reports the confirmed tap; the parent does the work.
/// </summary>
public partial class ConfirmActionCard : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    /// <summary>The section heading and the first button's text.</summary>
    [Parameter, EditorRequired] public string Title { get; set; } = string.Empty;

    /// <summary>One line under the heading: what the action does.</summary>
    [Parameter, EditorRequired] public string Why { get; set; } = string.Empty;

    /// <summary>The confirm question.</summary>
    [Parameter, EditorRequired] public string Ask { get; set; } = string.Empty;

    /// <summary>What gets deleted, shown with the question.</summary>
    [Parameter, EditorRequired] public string Warning { get; set; } = string.Empty;

    /// <summary>The red confirm button's text.</summary>
    [Parameter, EditorRequired] public string ConfirmText { get; set; } = string.Empty;

    [Parameter, EditorRequired] public string Icon { get; set; } = string.Empty;

    /// <summary>An outlined first button instead of a filled one, for the second of two such cards.</summary>
    [Parameter] public bool Outlined { get; set; }

    /// <summary>True while the work runs; the buttons are disabled.</summary>
    [Parameter] public bool Busy { get; set; }

    /// <summary>Shown above the confirm button when the last try failed.</summary>
    [Parameter] public string? Error { get; set; }

    [Parameter] public EventCallback OnConfirm { get; set; }

    private bool _asking;
}
