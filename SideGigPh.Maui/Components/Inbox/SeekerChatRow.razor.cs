using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Inbox;

/// <summary>A chat in the Seeker's Inbox (and on Seeker home): the skilled worker, their trade, the last message.</summary>
public partial class SeekerChatRow : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    [Parameter, EditorRequired] public InboxThread Thread { get; set; } = default!;
}
