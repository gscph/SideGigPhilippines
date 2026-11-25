using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;

namespace SideGigPh.Maui.Components.Professional;

/// <summary>A chat in the pro's Inbox (and on Pro home): the customer, the job and town, the last message.</summary>
public partial class ProChatRow : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;
    [Inject] private ProWorkspace Workspace { get; set; } = default!;

    [Parameter, EditorRequired] public Chat Chat { get; set; } = default!;
}
