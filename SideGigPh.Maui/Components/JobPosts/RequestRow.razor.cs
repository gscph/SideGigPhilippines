using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.JobPosts;

/// <summary>
/// One customer request as a short row in the worker's lists (Requests, My offers, Pro home), Airbnb Trips style:
/// a small picture (the customer's first photo, else the service's own), the service with the budget on the right in
/// money green, the place and distance, then the day the customer needs it (when they picked one), how long ago and
/// where it stands, and one button, "Offer" or "My offer".
/// The whole row is one tap target: it opens the request's own page (<see cref="RequestDetailPage"/>), where the
/// description, the photos and every offer action live. Lists put a <c>MudDivider</c> between rows.
/// </summary>
public partial class RequestRow : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;
    [Inject] private TimeProvider Clock { get; set; } = default!;

    [Parameter, EditorRequired] public RequestItem Item { get; set; } = default!;

    private string TimeLine => T.RequestTimeLine(Item, PostDay.Today(Clock));
}
