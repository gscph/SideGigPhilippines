namespace SideGigPh.Maui.Services;

/// <summary>
/// What a worker's Send offer (or Save, after Change) on a request's page would send: the customer's budget when they
/// take it, else the price and note they typed. <see cref="IsValid"/> says whether the button may send it.
/// </summary>
public sealed record OfferDraft(int PricePesos, string? Note, bool IsValid)
{
    /// <param name="budgetPesos">The customer's top budget, what "Take it" offers.</param>
    /// <param name="otherPrice">The worker chose "Offer another price" (or is changing a sent offer).</param>
    /// <param name="typedPesos">The price typed, or null when the box is empty.</param>
    /// <param name="typedNote">The note typed, or null.</param>
    public static OfferDraft Of(int budgetPesos, bool otherPrice, int? typedPesos, string? typedNote)
    {
        if (!otherPrice)
        {
            return new OfferDraft(budgetPesos, null, JobBoard.CanOffer(budgetPesos, null));
        }

        var note = string.IsNullOrWhiteSpace(typedNote) ? null : typedNote.Trim();
        return new OfferDraft(typedPesos ?? 0, note, JobBoard.CanOffer(typedPesos, note));
    }
}
