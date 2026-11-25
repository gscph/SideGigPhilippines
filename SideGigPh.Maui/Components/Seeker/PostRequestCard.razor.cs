using Microsoft.AspNetCore.Components;

namespace SideGigPh.Maui.Components.Seeker;

/// <summary>
/// "Post what you need done · Workers near you send their price ›" on Seeker home, one tappable row like Airbnb's promo
/// cards (no button: the search pill is home's main action), so a household sees from the first screen that it can post
/// instead of searching. Opens the post form, which starts with their last search's trade, job, town and budget.
/// </summary>
public partial class PostRequestCard : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;
}
