using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Components.JobPosts;

/// <summary>
/// The top of a request's card on its own page, like the listing on Airbnb's "Review and continue": the customer's
/// first photo (else the service's picture) on the left, then the post's title, the service under it as a hint, the place and distance, and the customer's
/// stars (when they have any) with how long ago it was posted.
/// </summary>
public partial class RequestSummary : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    [Parameter, EditorRequired] public JobPost Post { get; set; } = default!;
}
