using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Chats;

/// <summary>
/// One chat message: mine on the right in an ink bubble, theirs on the left in a gray one, with "Just now" / "3 hr ago"
/// under it when it is the last of a sender's run (<see cref="ChatMessages"/>). A logged call shows as "You called" /
/// "Called you". A pin is the customer's location: on the worker's side a tap opens <see cref="Place"/> in the
/// phone's map app (Google Maps or Waze, whichever is installed, else the browser); once the place is gone (the job
/// done or cancelled) it says so.
/// </summary>
public partial class ChatBubble : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;
    [Inject] private IMapLauncher Maps { get; set; } = default!;

    [Parameter, EditorRequired] public ChatMessage Message { get; set; } = default!;

    [Parameter] public bool Mine { get; set; }

    /// <summary>Shows "3 hr ago" under the bubble (the last message before the other person talks).</summary>
    [Parameter] public bool ShowTime { get; set; } = true;

    /// <summary>Spacing above the bubble (tighter inside one sender's run).</summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>The customer's place a pin opens (the picked job's), or null when it is no longer shared.</summary>
    [Parameter] public GeoPoint? Place { get; set; }

    /// <summary>The name the map app shows on the pin.</summary>
    [Parameter] public string? PlaceLabel { get; set; }

    private bool _busy;
    private bool _failed;

    private async Task OpenAsync()
    {
        if (_busy || Place is not { } place)
        {
            return; // a second tap before the button re-rendered as disabled
        }

        _busy = true;
        try
        {
            _failed = !await Maps.OpenAsync(place, PlaceLabel ?? T.Get(Copy.PinTheirs));
        }
        finally
        {
            _busy = false;
        }
    }
}
