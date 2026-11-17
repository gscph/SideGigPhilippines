using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Services;

namespace SideGigPh.Maui.Components.Onboarding;

/// <summary>
/// A trade's jobs as answer tiles a Skilled Worker ticks, several at once, or every one with "Select all"
/// (<see cref="OnboardingSteps.ToggleAllJobs"/>). It only reports taps; the parent decides.
/// </summary>
public partial class JobPicker : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    /// <summary>The trade whose jobs are shown.</summary>
    [Parameter] public string? CategoryId { get; set; }

    [Parameter] public IReadOnlyCollection<string> SelectedIds { get; set; } = [];

    /// <summary>Every listed job is ticked: the button reads "Remove all".</summary>
    [Parameter] public bool AllSelected { get; set; }

    /// <summary>A job was tapped.</summary>
    [Parameter] public EventCallback<string> OnPick { get; set; }

    /// <summary>"Select all" / "Remove all" was tapped.</summary>
    [Parameter] public EventCallback OnPickAll { get; set; }

    // With one listed job or none, "Select all" would do no more than a tap on the tile.
    private bool ShowSelectAll => OnboardingSteps.ListedJobs(CategoryId).Count > 1;

    private IReadOnlyList<ChoiceItem> Tiles => ChoiceItem.From(ReferenceData.ServicesOf(CategoryId), T);
}
