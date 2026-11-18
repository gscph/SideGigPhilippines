using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services;

/// <summary>Where a request stands for this worker: the row's status words and the detail page's bottom bar follow it.</summary>
public enum RequestState
{
    /// <summary>Open, and this worker hasn't offered yet: "Offer".</summary>
    New,

    /// <summary>Open, and this worker's offer waits for the customer: "My offer".</summary>
    Offered,

    /// <summary>The customer picked this worker: their chat is open.</summary>
    Picked,

    /// <summary>The post closed without picking this worker.</summary>
    NotPicked,
}

/// <summary>
/// A customer's request as the worker's short row (<c>RequestRow</c>) and its detail page show it: the post, how it
/// stands for this worker, their price and note when they offered, and the chat once picked. Built from a feed post
/// (<see cref="From(JobPost)"/>) or one of the worker's own offers (<see cref="From(MyOffer)"/>).
/// </summary>
public sealed record RequestItem(JobPost Post, RequestState State, int? MyPricePesos = null, string? MyNote = null, string? ChatId = null)
{
    /// <summary>A post from the feed: open, offered when the worker already sent a price.</summary>
    public static RequestItem From(JobPost post) =>
        new(post, post.MyOfferPesos is null ? RequestState.New : RequestState.Offered, post.MyOfferPesos);

    /// <summary>One of the worker's own offers (Requests → My offers).</summary>
    public static RequestItem From(MyOffer offer) => new(
        offer.Post,
        offer.State switch
        {
            MyOfferState.Picked => RequestState.Picked,
            MyOfferState.NotPicked => RequestState.NotPicked,
            _ => RequestState.Offered,
        },
        offer.PricePesos,
        string.IsNullOrWhiteSpace(offer.Note) ? null : offer.Note,
        offer.ChatId);

    /// <summary>The worker can still send, change or take back a price: the post is open.</summary>
    public bool CanOffer => State is RequestState.New or RequestState.Offered;

    /// <summary>The row's button says "My offer" (else "Offer"): the worker already sent a price on it.</summary>
    public bool HasMyOffer => State != RequestState.New;

    /// <summary>After the worker's price went through.</summary>
    public RequestItem Offered(int pricePesos, string? note) => this with
    {
        State = RequestState.Offered,
        MyPricePesos = pricePesos,
        MyNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
        Post = Post with { MyOfferPesos = pricePesos },
    };

    /// <summary>After the worker took their offer back: open to offer again.</summary>
    public RequestItem Withdrawn() => this with
    {
        State = RequestState.New,
        MyPricePesos = null,
        MyNote = null,
        Post = Post with { MyOfferPesos = null },
    };
}
