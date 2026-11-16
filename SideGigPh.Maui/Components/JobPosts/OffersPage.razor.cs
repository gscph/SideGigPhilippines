using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using SideGigPh.Maui.Components.Seeker;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.JobPosts;

/// <summary>
/// The offers on one of the household's posts (inDrive style): each worker's card (photo, stars, badges), their price
/// and note, how far they are; the picked one first, then the lowest price. "Pick" asks once more, then closes the
/// post, sends the worker a first message ("I picked your offer of ₱…") and opens the chat with them.
/// </summary>
public partial class OffersPage : ComponentBase
{
    [Inject] private JobBoard Board { get; set; } = default!;
    [Inject] private SeekerWorkspace Workspace { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;
    [Inject] private ILogger<OffersPage> Log { get; set; } = default!;

    [Parameter] public long PostId { get; set; }

    private JobPost? _post;
    private IReadOnlyList<JobOffer> _offers = [];
    private bool _loaded;
    private bool _busy;
    private long? _confirming;
    private LocalizedText? _notice;

    private string ServiceLabel =>
        _post is not null && ReferenceData.FindService(_post.ServiceId) is { } service ? T.Get(service.Label) : string.Empty;

    protected override async Task OnParametersSetAsync()
    {
        _loaded = false;
        _post = await Board.FindMineAsync(PostId);
        _offers = _post is null ? [] : await Board.LoadOffersAsync(PostId);
        _loaded = true;
    }

    private async Task PickAsync(JobOffer offer)
    {
        if (_busy || _post is null)
        {
            return; // a second tap before the button re-rendered
        }

        _busy = true;
        _notice = null;
        try
        {
            var result = await Board.PickAsync(offer);
            if (result != OfferResult.Done)
            {
                _notice = result == OfferResult.Gone ? Copy.OfferPickGone : Copy.OfferPickFailed;
                _confirming = null;
                return;
            }

            await SayPickedAsync(offer, _post);
            Nav.NavigateTo(SeekerRoutes.Thread(offer.Pro.Id));
        }
        finally
        {
            _busy = false;
        }
    }

    /// <summary>The first message to the picked worker. When it doesn't send, the chat opens anyway and they can write it.</summary>
    private async Task SayPickedAsync(JobOffer offer, JobPost post)
    {
        try
        {
            await Workspace.EnsureLoadedAsync();
            await Workspace.SendAsync(offer.Pro, T.Format(Copy.OfferPickMessage, Money.Peso(offer.PricePesos), ServiceLabel));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            Log.LogWarning(ex, "The first message after picking offer {OfferId} on post {PostId} didn't send", offer.Id, post.Id);
        }
    }
}
