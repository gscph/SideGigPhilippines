using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;

namespace SideGigPh.Maui.Components.Chats;

/// <summary>
/// The conversation in an open chat (either side): a "Today" / "Yesterday" / date divider per day, a sender's messages
/// in a row kept close with "3 hr ago" once under the last one, more room where the other person starts
/// (<see cref="ChatTimeline"/>).
/// </summary>
public partial class ChatMessages : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;
    [Inject] private TimeProvider Clock { get; set; } = default!;

    [Parameter, EditorRequired] public IReadOnlyList<ChatMessage> Messages { get; set; } = [];

    /// <summary>The id of the person reading, whose messages sit on the right.</summary>
    [Parameter, EditorRequired] public string MyId { get; set; } = string.Empty;

    /// <summary>The customer's place a "Customer's location" message opens (the picked job's; null when none is shared).</summary>
    [Parameter] public GeoPoint? Place { get; set; }

    /// <summary>The name the map app shows on that place.</summary>
    [Parameter] public string? PlaceLabel { get; set; }

    private DateOnly Today => ChatTimeline.DayOf(Clock.GetUtcNow());
}
