using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Seeker;

/// <summary>"Your last search: Pa-linis ng aircon · Taytay  [See pros]" on Seeker home.</summary>
public partial class LastSearchCard : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    [Parameter, EditorRequired] public string Summary { get; set; } = string.Empty;
}
