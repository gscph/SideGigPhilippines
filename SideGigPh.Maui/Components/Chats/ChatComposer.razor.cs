using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Chats;

/// <summary>
/// The bottom of a chat, pinned above the tabs: the message box and Send, with Call next to it
/// when the parent handles <see cref="OnCall"/> (the Seeker side). The parent does the sending.
/// </summary>
public partial class ChatComposer : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    [Parameter] public string? Message { get; set; }

    [Parameter] public EventCallback<string> MessageChanged { get; set; }

    /// <summary>Disables the buttons while a message is sending or the dialer is opening.</summary>
    [Parameter] public bool Busy { get; set; }

    [Parameter] public EventCallback OnSend { get; set; }

    /// <summary>Shows the Call button when set.</summary>
    [Parameter] public EventCallback OnCall { get; set; }
}
