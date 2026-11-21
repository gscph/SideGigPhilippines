using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Chats;

/// <summary>
/// One chat in an Inbox (either side): the other person's photo and name, an optional detail
/// line (their trade, or the job), and the last message ("You: Hi po…", "Called you"). Unread
/// chats get a red dot and bold name.
/// </summary>
public partial class ChatRow : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    [Parameter, EditorRequired] public string Href { get; set; } = string.Empty;

    /// <summary>The other person's name.</summary>
    [Parameter, EditorRequired] public string Name { get; set; } = string.Empty;

    [Parameter] public string? PhotoUrl { get; set; }

    [Parameter] public string? Detail { get; set; }

    [Parameter, EditorRequired] public ChatMessage LastMessage { get; set; } = default!;

    /// <summary>The id of the person looking at the Inbox, to tell their messages from the other person's.</summary>
    [Parameter, EditorRequired] public string MyId { get; set; } = string.Empty;

    [Parameter] public bool Unread { get; set; }

    private string Preview
    {
        get
        {
            var mine = LastMessage.SenderId == MyId;
            if (LastMessage.Kind == ChatMessageKind.Call)
            {
                return T.Get(mine ? Copy.YouCalled : Copy.CalledYou);
            }

            if (LastMessage.Kind == ChatMessageKind.Pin)
            {
                return T.Get(mine ? Copy.PinMine : Copy.PinTheirs);
            }

            return mine ? T.Format(Copy.InboxYouWrote, LastMessage.Text ?? string.Empty) : LastMessage.Text ?? string.Empty;
        }
    }
}
