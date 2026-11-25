using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.JobPosts;

/// <summary>
/// The top of the new post's card, like the listing on Airbnb's "Review and continue": the first photo of the problem
/// (else the service's picture), the service and its trade under it, and the gray Change for What (the service is
/// not repeated in a row below).
/// </summary>
public partial class PostDraftSummary : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    [Parameter, EditorRequired] public JobPostDraft Draft { get; set; } = default!;

    /// <summary>The gray button on the right ("Change"), which opens the What sheet.</summary>
    /// <summary>The button's words; empty: no button (Edit post once offers came in).</summary>
    [Parameter] public string? ActionLabel { get; set; }

    [Parameter] public EventCallback OnAction { get; set; }

    /// <summary>The page is sending: the button waits.</summary>
    [Parameter] public bool Busy { get; set; }
}
