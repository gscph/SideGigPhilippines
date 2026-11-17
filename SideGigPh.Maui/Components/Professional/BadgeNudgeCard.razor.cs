using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Professional;

/// <summary>
/// The amber reminder to add the badges (ID, TESDA, a past project): optional, but each one ranks the worker higher
/// in search (<c>SearchRanker.TrustFeature</c>) and customers trust a checked worker more. Shown while a badge can
/// still be asked for (<see cref="ProfileProgress.ShowBadgeNudge"/>); opens the badges page.
/// </summary>
public partial class BadgeNudgeCard : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    /// <summary>How many kinds of badge the worker already holds.</summary>
    [Parameter] public int BadgeCount { get; set; }
}
