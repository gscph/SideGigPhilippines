using Microsoft.AspNetCore.Components;

namespace SideGigPh.Admin.Components.Pages;

/// <summary>The skilled workers (<see cref="Worker.Views.WorkerListView"/>), opened with <c>?search=&amp;filter=</c> when given.</summary>
public partial class Workers
{
    [SupplyParameterFromQuery] public string? Search { get; set; }
    [SupplyParameterFromQuery] public string? Filter { get; set; }
}
