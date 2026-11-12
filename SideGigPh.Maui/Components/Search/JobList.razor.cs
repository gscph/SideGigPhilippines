using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Search;

/// <summary>
/// "Jobs they take" on the public profile: one job per line in full-size text with a check, easier to read
/// than the small chips the dense search card uses (<c>ServiceChips</c>).
/// </summary>
public partial class JobList : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    [Parameter, EditorRequired] public ProfessionalListing Listing { get; set; } = default!;

    private IReadOnlyList<string> Labels => T.JobLabels(Listing);
}
