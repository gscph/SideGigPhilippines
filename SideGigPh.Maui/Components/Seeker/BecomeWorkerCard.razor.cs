using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Seeker;

/// <summary>
/// "Become a Skilled Worker · Offer your skills and get customers near you ›" on the household's Me page while they have
/// never set up the worker side, like Airbnb's "Become a host" card: one tappable row in the same look as Home's
/// <see cref="PostRequestCard"/>, offering what they get rather than a bare "switch". Opens the worker interview through
/// <c>/switch/professional</c>; Back on its first question returns here (<c>SideSwitch.BackFrom</c>). Once the worker
/// side is set up, the Me page shows the plain "Switch to Skilled Worker" row instead.
/// </summary>
public partial class BecomeWorkerCard : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;
}
