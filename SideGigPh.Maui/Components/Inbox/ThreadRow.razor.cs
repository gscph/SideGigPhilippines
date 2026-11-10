using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Inbox;

/// <summary>One pro in the Inbox: their photo and name, and the last thing the Seeker sent ("You: Hi po…" / "You called").</summary>
public partial class ThreadRow : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    [Parameter, EditorRequired] public InboxThread Thread { get; set; } = default!;

    private string Preview => Thread.Latest is { Channel: InquiryChannel.Text, Message: { } message }
        ? T.Format(Copy.InboxYouWrote, message)
        : T.Get(Copy.YouCalled);
}
