using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Chats;

/// <summary>
/// The conversation in an open chat (either side): a "Today" / "Yesterday" / date divider per day instead of a time
/// under every bubble, a sender's messages in a row kept close, more room where the other person starts
/// (<see cref="ChatTimeline"/>).
/// </summary>
public partial class ChatMessages : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;
    [Inject] private TimeProvider Clock { get; set; } = default!;

    [Parameter, EditorRequired] public IReadOnlyList<ChatMessage> Messages { get; set; } = [];

    /// <summary>The id of the person reading, whose messages sit on the right.</summary>
    [Parameter, EditorRequired] public string MyId { get; set; } = string.Empty;

    private DateOnly Today => ChatTimeline.DayOf(Clock.GetUtcNow());
}
