using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.JobPosts;

/// <summary>
/// One worker's offer on the household's post: the worker's card as in search (photo, stars, badges), then their
/// price (green), how far they are and their note, with "See profile" and "Pick". "Pick" asks once more
/// (<see cref="Confirming"/>) before the page picks.
/// </summary>
public partial class OfferCard : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    [Parameter, EditorRequired] public JobOffer Offer { get; set; } = default!;

    /// <summary>The post is still open and nothing is sending.</summary>
    [Parameter] public bool CanPick { get; set; }

    /// <summary>Shows "Pick … for ₱…?" with Cancel and "Yes, pick" instead of the buttons.</summary>
    [Parameter] public bool Confirming { get; set; }

    /// <summary>The worker's public profile, whose Back returns here.</summary>
    [Parameter, EditorRequired] public string ProfileHref { get; set; } = string.Empty;

    [Parameter] public EventCallback OnPick { get; set; }

    [Parameter] public EventCallback OnCancel { get; set; }

    [Parameter] public EventCallback<JobOffer> OnConfirm { get; set; }

    private string? Highlight =>
        Offer.IsPicked ? T.Get(Copy.OfferPicked) : Offer.TakesBudget ? T.Get(Copy.OfferTakesBudget) : null;
}
