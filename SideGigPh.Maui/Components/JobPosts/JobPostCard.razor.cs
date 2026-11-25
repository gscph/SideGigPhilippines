using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.JobPosts;

/// <summary>
/// One of the household's own posts (the Seeker's Post tab) as a raised card that opens like Seeker home's "Post what
/// you need done": the first photo (else the megaphone, <see cref="Shared.PostThumb"/>), the service and trade, what is
/// wrong, the budget (green, like all money), the town and barangay when given, its photos and how long ago it was
/// posted. It says whether workers can still see it and offers "Take down"; "See N offers" on an open post and "Open
/// chat" once they picked a worker. Workers see posts as short rows instead (<see cref="RequestRow"/>).
/// </summary>
public partial class JobPostCard : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;
    [Inject] private TimeProvider Clock { get; set; } = default!;

    /// <summary>"Needed tomorrow", or null when any day is fine.</summary>
    private string? NeededText => T.Needed(Post.NeededOn, PostDay.Today(Clock));

    [Parameter, EditorRequired] public JobPost Post { get; set; } = default!;

    /// <summary>The household tapped "Take down".</summary>
    [Parameter] public EventCallback<JobPost> OnTakeDown { get; set; }

    private LocalizedText Status =>
        Post.PickedProId is not null ? Copy.PostPicked : Post.IsOpen ? Copy.PostShowing : Copy.PostTakenDown;

    private string OffersLabel => Post.OfferCount switch
    {
        0 => T.Get(Copy.PostOffersNone),
        1 => T.Get(Copy.PostOffersOne),
        var n => T.Format(Copy.PostOffersMany, n),
    };

    /// <summary>The trade's name as the post was made with it, or null for a trade we no longer offer.</summary>
    private string? TradeLabel => T.TradeLabel(Post);
}
