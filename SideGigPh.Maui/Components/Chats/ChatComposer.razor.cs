using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Chats;

/// <summary>
/// The bottom of a chat, pinned above the tabs: a pill message box and a round send button. The
/// parent does the sending (and shows Call in its <see cref="ChatHeader"/>).
/// </summary>
public partial class ChatComposer : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    [Parameter] public string? Message { get; set; }

    [Parameter] public EventCallback<string> MessageChanged { get; set; }

    /// <summary>Disables the send button while a message is sending.</summary>
    [Parameter] public bool Busy { get; set; }

    [Parameter] public EventCallback OnSend { get; set; }
}
