using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Inbox;

/// <summary>
/// The bottom of a thread, pinned above the tabs: an editable message (pre-written for the
/// Seeker) with Send text, and Call next to it. The parent does the sending.
/// </summary>
public partial class MessageComposer : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    [Parameter] public string? Message { get; set; }

    [Parameter] public EventCallback<string> MessageChanged { get; set; }

    /// <summary>Disables both buttons while the phone app is opening.</summary>
    [Parameter] public bool Busy { get; set; }

    [Parameter] public EventCallback OnSend { get; set; }

    [Parameter] public EventCallback OnCall { get; set; }
}
