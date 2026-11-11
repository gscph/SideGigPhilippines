using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Inbox;

/// <summary>The Seeker's Inbox: one chat per skilled worker they contacted, most recent message first.</summary>
public partial class InboxPage : ComponentBase
{
    [Inject] private SeekerWorkspace Workspace { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;

    protected override Task OnInitializedAsync() => Workspace.RefreshActivityAsync();
}
