using Microsoft.AspNetCore.Components;

namespace SideGigPh.Maui.Components.Chats;

/// <summary>
/// An open chat as its own full screen (outside the tab shell, like Messenger): the <see cref="ChatHeader"/> pinned at
/// the top, the conversation filling the page, and the <see cref="ChatComposer"/> pinned at the bottom, so the
/// messages get the whole height and nothing between them and the keyboard.
/// </summary>
public partial class ChatScreen : ComponentBase
{
    [Parameter] public RenderFragment? Header { get; set; }

    [Parameter] public RenderFragment? ChildContent { get; set; }

    [Parameter] public RenderFragment? Footer { get; set; }
}
