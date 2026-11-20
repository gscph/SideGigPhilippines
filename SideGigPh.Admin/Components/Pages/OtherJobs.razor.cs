using Microsoft.AspNetCore.Components;
using SideGigPh.Admin.Extensions;

namespace SideGigPh.Admin.Components.Pages;

/// <summary>
/// What people typed under "Others" in two tabs: Groups (<see cref="OtherJob.Views.OtherJobGroupsView"/>, the most
/// requested missing work first) and All entries (<see cref="OtherJob.Views.OtherJobListView"/>). Opened with
/// <c>?tab=all&amp;search=&amp;filter=&amp;days=</c> when given; the filter carries over between the tabs.
/// </summary>
public partial class OtherJobs
{
    /// <summary><c>?tab=all</c> opens All entries; Groups is the default.</summary>
    private const string AllTab = "all";

    [Inject] public required NavigationManager Navigation { get; set; }

    [SupplyParameterFromQuery] public string? Tab { get; set; }
    [SupplyParameterFromQuery] public string? Search { get; set; }
    [SupplyParameterFromQuery] public string? Filter { get; set; }
    [SupplyParameterFromQuery] public int? Days { get; set; }

    private int ActiveTab => Tab == AllTab ? 1 : 0;

    private void OnTabChanged(int index) => Navigation.KeepInAddress(("tab", index == 1 ? AllTab : null));
}
