using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.JobPosts;

/// <summary>
/// A worker's answer to a post, under the card in their feed (inDrive style): "Take it for ₱budget" in one tap, or
/// "Offer another price" (their price and an optional note). Once sent it shows their offer, waiting for the customer,
/// with "Change price" and "Take back". The page sends and takes back; this only collects what was typed.
/// </summary>
public partial class OfferBox : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    [Parameter, EditorRequired] public JobPost Post { get; set; } = default!;

    /// <summary>The page is sending: the buttons wait.</summary>
    [Parameter] public bool Busy { get; set; }

    /// <summary>Sends a price (the budget or their own); true when it went through.</summary>
    [Parameter, EditorRequired] public Func<OfferDraft, Task<bool>> Send { get; set; } = default!;

    /// <summary>The worker tapped "Take back".</summary>
    [Parameter] public EventCallback OnWithdraw { get; set; }

    private bool _editing;
    private int? _price;
    private string? _note;

    private Task TakeBudgetAsync() => Send(new OfferDraft(Post, Post.BudgetPesos, null));

    private void Edit()
    {
        _price = Post.MyOfferPesos ?? Post.BudgetPesos;
        _editing = true;
    }

    private void Cancel() => _editing = false;

    private async Task SendTypedAsync()
    {
        if (_price is not { } price || !JobBoard.CanOffer(price, _note))
        {
            return;
        }

        _editing = !await Send(new OfferDraft(Post, price, _note)); // stays open when it didn't send
    }
}
