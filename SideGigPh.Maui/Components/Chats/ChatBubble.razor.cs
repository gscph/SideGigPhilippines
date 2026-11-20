using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Chats;

/// <summary>
/// One chat message: mine on the right in an ink bubble, theirs on the left in a gray one, with "Just now" / "3 hr ago"
/// under it when it is the last of a sender's run (<see cref="ChatMessages"/>). A logged call shows as "You called" /
/// "Called you".
/// </summary>
public partial class ChatBubble : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    [Parameter, EditorRequired] public ChatMessage Message { get; set; } = default!;

    [Parameter] public bool Mine { get; set; }

    /// <summary>Shows "3 hr ago" under the bubble (the last message before the other person talks).</summary>
    [Parameter] public bool ShowTime { get; set; } = true;

    /// <summary>Spacing above the bubble (tighter inside one sender's run).</summary>
    [Parameter] public string? Class { get; set; }
}
