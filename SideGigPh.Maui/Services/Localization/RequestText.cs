using System.Globalization;
using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services.Localization;

/// <summary>
/// A customer's request in the worker's words, one short line each: what the row (<c>RequestRow</c>) and the detail
/// page (<c>RequestDetailPage</c>) print. Facts are joined by "·", and a line only names a state when there is one.
/// </summary>
public static class RequestText
{
    /// <summary>The service ("Linis ng aircon"), else the trade for a service we no longer list, else its id.</summary>
    public static string ServiceLabel(this AppText text, JobPost post) =>
        ReferenceData.FindService(post.ServiceId) is { } service ? text.Get(service.Label)
        : ReferenceData.FindTrade(post.CategoryId) is { } trade ? text.Get(trade.Label)
        : post.ServiceId;

    /// <summary>"Angono · Brgy. San Isidro · 3 km away": the town, its barangay when given, and how far when known.</summary>
    public static string RequestPlace(this AppText text, JobPost post)
    {
        var place = text.Place(post.CityId, post.BarangayId);
        return post.DistanceKm is { } km ? text.Format(Copy.PostDistance, place, km) : place;
    }

    /// <summary>What the worker did or what became of it ("You offered ₱1,000", "Picked you", "Not picked"); null for a new one.</summary>
    public static string? RequestStatus(this AppText text, RequestItem item) => item.State switch
    {
        RequestState.Offered when item.MyPricePesos is { } price => text.Format(Copy.RequestYouOffered, Money.Peso(price)),
        RequestState.Picked => text.Get(Copy.MyOfferPicked),
        RequestState.NotPicked => text.Get(Copy.MyOfferNotPicked),
        _ => null,
    };

    /// <summary>The row's small last line: "2 hr ago", then "· You offered ₱1,000" when there is a state.</summary>
    public static string RequestTimeLine(this AppText text, RequestItem item) =>
        text.RequestStatus(item) is { } status
            ? text.Format(Copy.RequestTimeAndStatus, text.Ago(item.Post.CreatedAt), status)
            : text.Ago(item.Post.CreatedAt);

    /// <summary>The row's one button: "Offer" until the worker sent a price, then "My offer".</summary>
    public static string RequestAction(this AppText text, RequestItem item) =>
        text.Get(item.HasMyOffer ? Copy.RequestMyOffer : Copy.RequestOffer);

    /// <summary>The customer's average stars as the stat row's value: "4.7".</summary>
    public static string StarsValue(ReviewSummary stars) => stars.Average.ToString("0.0", CultureInfo.InvariantCulture);
}
