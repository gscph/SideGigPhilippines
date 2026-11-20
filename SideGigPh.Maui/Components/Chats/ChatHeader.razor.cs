using Microsoft.AspNetCore.Components;

namespace SideGigPh.Maui.Components.Chats;

/// <summary>
/// The top of an open chat (either side), pinned while the messages scroll: back to the Inbox, the other person's photo, name and
/// detail line (their trade, or the job), an optional action on the right (Call), and a hairline.
/// </summary>
public partial class ChatHeader : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    [Parameter, EditorRequired] public string BackHref { get; set; } = string.Empty;

    [Parameter, EditorRequired] public string Name { get; set; } = string.Empty;

    [Parameter] public string? PhotoUrl { get; set; }

    [Parameter] public string? Detail { get; set; }

    /// <summary>
    /// Where tapping the photo or name goes (the pro's public profile, whose Back returns to this chat), shown by a ">"
    /// after the name; plain text when null.
    /// </summary>
    [Parameter] public string? ProfileHref { get; set; }

    [Parameter] public string? ProfileLabel { get; set; }

    [Parameter] public RenderFragment? Actions { get; set; }
}
