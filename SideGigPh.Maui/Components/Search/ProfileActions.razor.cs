using Microsoft.AspNetCore.Components;

namespace SideGigPh.Maui.Components.Search;

/// <summary>
/// The profile's Back and Report (🚩) buttons: in a plain row above a worker without work photos, or on white circles
/// over the <see cref="ProfileCover"/> photo.
/// </summary>
public partial class ProfileActions : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    [Parameter, EditorRequired] public string BackHref { get; set; } = string.Empty;

    /// <summary>The report page, or null on your own listing (nothing to report).</summary>
    [Parameter] public string? ReportHref { get; set; }

    /// <summary>True when the buttons sit on a photo, so they get a white circle.</summary>
    [Parameter] public bool OnPhoto { get; set; }

    private string? ButtonClass => OnPhoto ? "profile-cover-button" : null;
}
