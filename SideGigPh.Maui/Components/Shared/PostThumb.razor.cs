using Microsoft.AspNetCore.Components;

namespace SideGigPh.Maui.Components.Shared;

/// <summary>
/// The square picture at the start of a job post, in the same soft box as "Post what you need done" on Seeker home:
/// the household's first photo when they added one, else the megaphone (<see cref="Services.AppImages.PostRequest"/>).
/// </summary>
public partial class PostThumb : ComponentBase
{
    /// <summary>The post's first photo, or null for none.</summary>
    [Parameter] public string? PhotoUrl { get; set; }
}
