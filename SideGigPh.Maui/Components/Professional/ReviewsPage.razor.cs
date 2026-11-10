using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Professional;

/// <summary>What customers said about the pro: the star summary, then every review, newest first.</summary>
public partial class ReviewsPage : ComponentBase
{
    [Inject] private ProWorkspace Workspace { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;

    protected override Task OnInitializedAsync() => Workspace.RefreshActivityAsync();
}
