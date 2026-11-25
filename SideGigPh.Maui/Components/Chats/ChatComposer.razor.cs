using Microsoft.AspNetCore.Components;

namespace SideGigPh.Maui.Components.Chats;

/// <summary>
/// The bottom of a chat, pinned to the bottom of the screen (above the keyboard): a gray rounded message box and a
/// purple Send button with the word on it (clearer than an arrow for people who don't read icons). The
/// parent does the sending (and shows Call in its <see cref="ChatHeader"/>). Its <see cref="Error"/> ("didn't send")
/// sits inside the pinned box, so the box never covers it.
/// </summary>
public partial class ChatComposer : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    [Parameter] public string? Message { get; set; }

    [Parameter] public EventCallback<string> MessageChanged { get; set; }

    /// <summary>Disables the send button while a message is sending.</summary>
    [Parameter] public bool Busy { get; set; }

    [Parameter] public EventCallback OnSend { get; set; }

    /// <summary>A failure to show right above the message box (null: none).</summary>
    [Parameter] public string? Error { get; set; }
}
