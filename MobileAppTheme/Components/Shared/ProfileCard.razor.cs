using Microsoft.AspNetCore.Components;

namespace MobileAppTheme.Components.Shared;

/// <summary>The raised card at the top of a Me page: big photo (or initials), name and optional actions.</summary>
public partial class ProfileCard : ComponentBase
{
    [Parameter] public string? Name { get; set; }

    [Parameter] public string? PhotoUrl { get; set; }

    /// <summary>Under the name, e.g. a "Change photo" link.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }
}
