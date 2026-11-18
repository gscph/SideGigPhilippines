using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.JobPosts;

/// <summary>
/// "Offer another price" on a request's own page (<see cref="RequestDetailPage"/>): the worker's price (their earlier
/// offer, else the customer's budget, to start from) and an optional note, then Send or Cancel. The page sends; this
/// only collects what was typed.
/// </summary>
public partial class OfferBox : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    [Parameter, EditorRequired] public RequestItem Item { get; set; } = default!;

    /// <summary>The page is sending: the buttons wait.</summary>
    [Parameter] public bool Busy { get; set; }

    /// <summary>Sends the typed price and note.</summary>
    [Parameter, EditorRequired] public EventCallback<(int Price, string? Note)> OnSend { get; set; }

    /// <summary>The worker closed the box without sending.</summary>
    [Parameter] public EventCallback OnCancel { get; set; }

    private int? _price;
    private string? _note;

    protected override void OnInitialized()
    {
        _price = Item.MyPricePesos ?? Item.Post.BudgetPesos;
        _note = Item.MyNote;
    }

    private async Task SendTypedAsync()
    {
        if (_price is { } price && JobBoard.CanOffer(price, _note))
        {
            await OnSend.InvokeAsync((price, _note));
        }
    }
}
