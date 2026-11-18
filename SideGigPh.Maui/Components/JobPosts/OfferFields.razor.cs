using Microsoft.AspNetCore.Components;

namespace SideGigPh.Maui.Components.JobPosts;

/// <summary>
/// The worker's own price and an optional note for the customer, on a request's page (under "Offer another price", or
/// in place of the price row after Change). Only collects; the page's button sends (<see cref="RequestFooter"/>).
/// </summary>
public partial class OfferFields : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    [Parameter] public int? Price { get; set; }

    [Parameter] public EventCallback<int?> PriceChanged { get; set; }

    [Parameter] public string? Note { get; set; }

    [Parameter] public EventCallback<string?> NoteChanged { get; set; }

    /// <summary>The page is sending: the fields wait.</summary>
    [Parameter] public bool Busy { get; set; }
}
