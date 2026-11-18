using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Components.JobPosts;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Professional;

/// <summary>
/// A job request on Pro home: the service, the budget (green, like all money), the town and how far, how long ago.
/// Tapping it opens Job Requests, where the worker takes the budget or offers a price.
/// </summary>
public partial class ProRequestRow : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    [Parameter, EditorRequired] public JobPost Post { get; set; } = default!;

    private string ServiceLabel =>
        ReferenceData.FindService(Post.ServiceId) is { } service ? T.Get(service.Label)
        : ReferenceData.FindTrade(Post.CategoryId) is { } trade ? T.Get(trade.Label)
        : Post.ServiceId;

    private string Where
    {
        get
        {
            var town = T.Place(Post.CityId, Post.BarangayId);
            return Post.DistanceKm is { } km ? T.Format(Copy.PostDistance, town, km) : town;
        }
    }
}
