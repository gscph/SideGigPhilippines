using Microsoft.AspNetCore.Components;

namespace SideGigPh.Admin.Components.Pages;

/// <summary>What people typed under "Others" (<see cref="OtherJob.Views.OtherJobListView"/>), opened with <c>?search=&amp;filter=</c> when given.</summary>
public partial class OtherJobs
{
    [SupplyParameterFromQuery] public string? Search { get; set; }
    [SupplyParameterFromQuery] public string? Filter { get; set; }
}
