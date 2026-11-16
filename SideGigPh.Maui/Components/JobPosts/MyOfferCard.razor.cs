using Microsoft.AspNetCore.Components;
using MudBlazor;
using Color = MudBlazor.Color;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.JobPosts;

/// <summary>
/// One of the worker's own offers: the service, how it stands (waiting, picked, not picked), their price against the
/// customer's budget, the town and their note; a picked one opens the chat the household started.
/// </summary>
public partial class MyOfferCard : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    [Parameter, EditorRequired] public MyOffer Offer { get; set; } = default!;

    private string ServiceLabel => ReferenceData.FindService(Offer.Post.ServiceId) is { } service
        ? T.Get(service.Label)
        : ReferenceData.Find(ReferenceData.Categories, Offer.Post.CategoryId) is { } trade ? T.Get(trade.Label) : Offer.Post.ServiceId;

    private string Town => ReferenceData.Find(ReferenceData.Cities, Offer.Post.CityId) is { } town ? T.Get(town.Label) : Offer.Post.CityId;

    private LocalizedText StateText => Offer.State switch
    {
        MyOfferState.Picked => Copy.MyOfferPicked,
        MyOfferState.NotPicked => Copy.MyOfferNotPicked,
        _ => Copy.MyOfferWaiting,
    };

    private Color StateColor => Offer.State switch
    {
        MyOfferState.Picked => Color.Success,
        MyOfferState.Waiting => Color.Primary,
        _ => Color.Default,
    };

    private string StateIcon => Offer.State switch
    {
        MyOfferState.Picked => Icons.Material.Rounded.CheckCircle,
        MyOfferState.Waiting => Icons.Material.Rounded.HourglassEmpty,
        _ => Icons.Material.Rounded.Remove,
    };
}
