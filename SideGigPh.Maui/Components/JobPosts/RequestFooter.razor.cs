using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Services;

namespace SideGigPh.Maui.Components.JobPosts;

/// <summary>
/// The pinned bottom of a request's page, one action at a time: a new request's full-width Send offer; while changing a
/// sent offer, Cancel and Save (Airbnb's change sheet); once picked, Open chat. A waiting or not-picked offer has none.
/// The page does the sending.
/// </summary>
public partial class RequestFooter : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    [Parameter, EditorRequired] public RequestItem Item { get; set; } = default!;

    /// <summary>The worker is changing their sent offer.</summary>
    [Parameter] public bool Editing { get; set; }

    /// <summary>What was chosen or typed can be sent.</summary>
    [Parameter] public bool CanSend { get; set; }

    /// <summary>The page is sending: the buttons wait.</summary>
    [Parameter] public bool Busy { get; set; }

    /// <summary>Send offer, or Save after Change.</summary>
    [Parameter] public EventCallback OnSend { get; set; }

    /// <summary>Cancel while changing.</summary>
    [Parameter] public EventCallback OnCancel { get; set; }

    private bool HasButtons => Editing || Item.State == RequestState.New || (Item.State == RequestState.Picked && Item.ChatId is not null);
}
