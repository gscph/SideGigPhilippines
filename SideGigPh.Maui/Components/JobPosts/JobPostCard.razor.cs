using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Components.Seeker;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.JobPosts;

/// <summary>
/// One job post as a card: the service and trade, what is wrong, the budget (green, like all money), the town (for
/// a worker also how far, in whole kilometres), its photos and how long ago it was posted. For its household
/// (<see cref="ForWorker"/> false) it says whether workers can still see it and offers "Take down"; for a worker it
/// marks a service they offer and has a flag to report a fake post. Offers: the household gets "See N offers" on an
/// open post and "Open chat" once they picked a worker; the worker answers under the card (<see cref="OfferBox"/>).
/// </summary>
public partial class JobPostCard : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    [Parameter, EditorRequired] public JobPost Post { get; set; } = default!;

    /// <summary>Shown in a worker's feed (distance, "You offer this", report) instead of the household's own list.</summary>
    [Parameter] public bool ForWorker { get; set; }

    /// <summary>The household tapped "Take down".</summary>
    [Parameter] public EventCallback<JobPost> OnTakeDown { get; set; }

    /// <summary>A worker tapped the report flag.</summary>
    [Parameter] public EventCallback<JobPost> OnReport { get; set; }

    /// <summary>For a worker: sends their offer (true when it went through). Without it the card has no offer buttons.</summary>
    [Parameter] public Func<OfferDraft, Task<bool>>? SendOffer { get; set; }

    /// <summary>A worker tapped "Take back" on their offer.</summary>
    [Parameter] public EventCallback<JobPost> OnWithdrawOffer { get; set; }

    /// <summary>The page is sending: the offer buttons wait.</summary>
    [Parameter] public bool Busy { get; set; }

    private LocalizedText Status =>
        Post.PickedProId is not null ? Copy.PostPicked : Post.IsOpen ? Copy.PostShowing : Copy.PostTakenDown;

    private string OffersLabel => Post.OfferCount switch
    {
        0 => T.Get(Copy.PostOffersNone),
        1 => T.Get(Copy.PostOffersOne),
        var n => T.Format(Copy.PostOffersMany, n),
    };

    private string ServiceLabel =>
        ReferenceData.FindService(Post.ServiceId) is { } service ? T.Get(service.Label) : TradeLabel ?? Post.ServiceId;

    /// <summary>The trade's name, or null for a trade we no longer offer.</summary>
    private string? TradeLabel =>
        ReferenceData.Find(ReferenceData.Categories, Post.CategoryId) is { } trade ? T.Get(trade.Label) : null;

    private string Town =>
        ReferenceData.Find(ReferenceData.Cities, Post.CityId) is { } town ? T.Get(town.Label) : Post.CityId;

    private string Where => ForWorker && Post.DistanceKm is { } km ? T.Format(Copy.PostDistance, Town, km) : Town;
}
