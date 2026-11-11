using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Chats;

/// <summary>
/// One chat message: mine on the right in the brand tint, theirs on the left outlined. A logged
/// call shows as "You called" / "Called you".
/// </summary>
public partial class ChatBubble : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    [Parameter, EditorRequired] public ChatMessage Message { get; set; } = default!;

    [Parameter] public bool Mine { get; set; }
}
