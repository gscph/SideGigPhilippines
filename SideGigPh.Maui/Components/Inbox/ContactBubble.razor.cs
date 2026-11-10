using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Inbox;

/// <summary>One thing the Seeker sent a pro, as a right-aligned chat bubble: the text, or "You called".</summary>
public partial class ContactBubble : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    [Parameter, EditorRequired] public Inquiry Contact { get; set; } = default!;

    private string Caption => Contact.Channel == InquiryChannel.Text
        ? T.Format(Copy.SentBySms, T.Ago(Contact.At))
        : T.Ago(Contact.At);
}
