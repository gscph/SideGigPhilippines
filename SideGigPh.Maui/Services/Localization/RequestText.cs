using System.Globalization;
using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services.Localization;

/// <summary>
/// A customer's request in the worker's words, one short line each: what the row (<c>RequestRow</c>) and the detail
/// page (<c>RequestDetailPage</c>) print. Facts are joined by "·", and a line only names a state when there is one.
/// </summary>
public static class RequestText
{
    /// <summary>
    /// The service ("Linis ng aircon") as the post was made with it (an operator's later rename doesn't change a post),
    /// else today's name, else the trade for a service we no longer list, else its id.
    /// </summary>
    public static string ServiceLabel(this AppText text, JobPost post) =>
        post.ServiceName is { } saved ? text.Get(saved)
        : ReferenceData.FindService(post.ServiceId) is { } service ? text.Get(service.Label)
        : text.TradeLabel(post) ?? post.ServiceId;

    /// <summary>The trade ("Aircon Technician") as the post was made with it, else today's name, or null for a trade we no longer offer.</summary>
    public static string? TradeLabel(this AppText text, JobPost post) =>
        post.TradeName is { } saved ? text.Get(saved)
        : ReferenceData.FindTrade(post.CategoryId) is { } trade ? text.Get(trade.Label)
        : null;

    /// <summary>A picked job's service as its post had it, else today's name, else the "Others" trade's, else its id.</summary>
    public static string ServiceLabel(this AppText text, PickedJob job) =>
        job.ServiceName is { } saved ? text.Get(saved)
        : (ReferenceData.FindService(job.ServiceId) ?? ReferenceData.FindTrade(job.ServiceId)) is { } service ? text.Get(service.Label)
        : job.ServiceId;

    /// <summary>The post's header: the household's own title, else (a post made before titles) the service.</summary>
    public static string PostTitle(this AppText text, JobPost post) =>
        string.IsNullOrWhiteSpace(post.Title) ? text.ServiceLabel(post) : post.Title;

    /// <summary>The service as a hint under the title ("Linis ng aircon"), or null when the service is already the header.</summary>
    public static string? PostServiceHint(this AppText text, JobPost post) =>
        string.IsNullOrWhiteSpace(post.Title) ? null : text.ServiceLabel(post);

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

    /// <summary>
    /// The row's small last line: "Needed tomorrow" when the customer picked a day, "2 hr ago", then "· You offered
    /// ₱1,000" when there is a state.
    /// </summary>
    public static string RequestTimeLine(this AppText text, RequestItem item, DateOnly today)
    {
        var line = text.RequestStatus(item) is { } status
            ? text.Format(Copy.RequestTimeAndStatus, text.Ago(item.Post.CreatedAt), status)
            : text.Ago(item.Post.CreatedAt);
        return text.Needed(item.Post.NeededOn, today) is { } needed ? text.Format(Copy.RequestTimeAndStatus, needed, line) : line;
    }

    /// <summary>The row's one button: "Offer" until the worker sent a price, then "My offer".</summary>
    public static string RequestAction(this AppText text, RequestItem item) =>
        text.Get(item.HasMyOffer ? Copy.RequestMyOffer : Copy.RequestOffer);

    /// <summary>The request page's big title, what to do or what became of it: "Send your price", "Your offer", "Picked you", "Not picked".</summary>
    public static string RequestPageTitle(this AppText text, RequestItem item) => text.Get(item.State switch
    {
        RequestState.New => Copy.RequestSendTitle,
        RequestState.Offered => Copy.RequestOfferedTitle,
        RequestState.Picked => Copy.MyOfferPicked,
        _ => Copy.MyOfferNotPicked,
    });

    /// <summary>The summary's last line: the customer's stars when they have any, then how long ago ("4.7 · 2 hr ago").</summary>
    public static string RequestMeta(this AppText text, JobPost post) =>
        post.HouseholdStars is { } stars
            ? text.Format(Copy.RequestTimeAndStatus, StarsValue(stars), text.Ago(post.CreatedAt))
            : text.Ago(post.CreatedAt);

    /// <summary>
    /// The muted line under the worker's price: "Waiting for the customer" while open (the title already says picked or
    /// not), then their note when they wrote one; null when there is nothing to add.
    /// </summary>
    public static string? RequestOfferDetail(this AppText text, RequestItem item)
    {
        var status = item.State == RequestState.Offered ? text.Get(Copy.RequestWaiting) : null;
        return (status, item.MyNote) switch
        {
            ({ } s, { } note) => text.Format(Copy.RequestStatusAndNote, s, note),
            (null, { } note) => note,
            _ => status,
        };
    }

    /// <summary>The customer's average stars: "4.7".</summary>
    public static string StarsValue(ReviewSummary stars) => stars.Average.ToString("0.0", CultureInfo.InvariantCulture);
}
