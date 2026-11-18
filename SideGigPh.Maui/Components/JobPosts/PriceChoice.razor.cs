using Microsoft.AspNetCore.Components;

namespace SideGigPh.Maui.Components.JobPosts;

/// <summary>
/// "Your price" on a new request, like Airbnb's "Choose when to pay": two choices in one card, "Take it for ₱budget"
/// (picked at first) or "Offer another price", which opens <see cref="OfferFields"/> under it. Only collects; the
/// page's Send offer button sends.
/// </summary>
public partial class PriceChoice : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    /// <summary>The customer's top budget, what "Take it" offers.</summary>
    [Parameter, EditorRequired] public int BudgetPesos { get; set; }

    /// <summary>The worker chose "Offer another price".</summary>
    [Parameter] public bool OtherPrice { get; set; }

    [Parameter] public EventCallback<bool> OtherPriceChanged { get; set; }

    [Parameter] public int? Price { get; set; }

    [Parameter] public EventCallback<int?> PriceChanged { get; set; }

    [Parameter] public string? Note { get; set; }

    [Parameter] public EventCallback<string?> NoteChanged { get; set; }

    /// <summary>The page is sending: the choices wait.</summary>
    [Parameter] public bool Busy { get; set; }
}
