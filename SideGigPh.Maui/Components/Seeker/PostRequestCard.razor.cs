using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Seeker;

/// <summary>
/// "Post what you need done · Say your budget. Workers near you send their price. [Post]" on Seeker home, so a
/// household sees from the first screen that it can post instead of searching. Opens the post form, which starts
/// with their last search's trade, job, town and budget.
/// </summary>
public partial class PostRequestCard : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;
}
