using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Services;

namespace SideGigPh.Maui.Components.JobPosts;

/// <summary>
/// The bottom bar of a request's own page, Airbnb style (price on the left, one main button on the right). New: the
/// customer's budget, "Take it" and the "Offer another price" link. Offered: "Your offer: ₱X · Waiting for the customer"
/// with Take back and Change. Picked: "Picked you" and Open chat. Not picked: just that. The page does the sending.
/// </summary>
public partial class RequestActionBar : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    [Parameter, EditorRequired] public RequestItem Item { get; set; } = default!;

    /// <summary>The page is sending: the buttons wait.</summary>
    [Parameter] public bool Busy { get; set; }

    /// <summary>"Take it": an offer at the customer's budget.</summary>
    [Parameter] public EventCallback OnTake { get; set; }

    /// <summary>"Offer another price" or "Change": opens the price box.</summary>
    [Parameter] public EventCallback OnOtherPrice { get; set; }

    /// <summary>"Take back" on the worker's offer.</summary>
    [Parameter] public EventCallback OnWithdraw { get; set; }
}
