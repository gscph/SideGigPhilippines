using Microsoft.AspNetCore.Components;

namespace SideGigPh.Admin.Components.Pages;

/// <summary>The households (<see cref="Seeker.Views.SeekerListView"/>), opened with <c>?search=&amp;town=</c> when given.</summary>
public partial class Seekers
{
    [SupplyParameterFromQuery] public string? Search { get; set; }
    [SupplyParameterFromQuery] public string? Town { get; set; }
}
