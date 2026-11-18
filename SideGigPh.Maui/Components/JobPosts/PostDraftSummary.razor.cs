using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.JobPosts;

/// <summary>
/// The top of the new post's card, like the listing on Airbnb's "Review and continue": the first photo of the problem
/// (else the service's picture), the service and its trade under it.
/// </summary>
public partial class PostDraftSummary : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    [Parameter, EditorRequired] public JobPostDraft Draft { get; set; } = default!;
}
