using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Services;

namespace SideGigPh.Maui.Components.Seeker;

/// <summary>
/// The Seeker's reviews, opened from their Me page: pros they contacted and can rate now, then the reviews they
/// already wrote (tap one to change it). Only contacted pros can be rated.
/// </summary>
public partial class SeekerReviewsPage : ComponentBase
{
    [Inject] private SeekerWorkspace Workspace { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;

    protected override Task OnInitializedAsync() => Workspace.RefreshIfStaleAsync();
}
