using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.JobPosts;

/// <summary>
/// The pro's Requests tab: what customers posted for their trade within the distance they travel, a service they
/// offer first, then nearest, then newest, a page at a time. Until the listing is published it says to publish
/// first (the server matches posts to the published listing). The flag on a card reports a fake post. Under each card
/// the worker takes the budget or offers another price, changes it or takes it back.
/// </summary>
public partial class ProPostsPage : ComponentBase, IDisposable
{
    [Inject] private JobBoard Board { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;

    private bool _loaded;
    private bool _busy;
    private LocalizedText? _notice;
    private bool _noticeIsError;

    protected override async Task OnInitializedAsync()
    {
        Board.Changed += OnChanged;
        await Board.LoadFeedAsync();
        _loaded = true;
    }

    public void Dispose() => Board.Changed -= OnChanged;

    private void OnChanged() => _ = InvokeAsync(StateHasChanged);

    private async Task ReportAsync(JobPost post)
    {
        if (_busy)
        {
            return; // a second tap before the button re-rendered
        }

        _busy = true;
        try
        {
            var reported = await Board.ReportAsync(post);
            _notice = reported ? Copy.PostReported : Copy.PostReportFailed;
            _noticeIsError = !reported;
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task<bool> SendOfferAsync(OfferDraft offer)
    {
        if (_busy)
        {
            return false; // a second tap before the button re-rendered
        }

        _busy = true;
        try
        {
            var result = await Board.SendOfferAsync(offer.Post, offer.PricePesos, offer.Note);
            Notify(result, Copy.OfferSentNotice);
            return result == OfferResult.Done;
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task WithdrawOfferAsync(JobPost post)
    {
        if (_busy)
        {
            return; // a second tap before the button re-rendered
        }

        _busy = true;
        try
        {
            Notify(await Board.WithdrawOfferAsync(post), Copy.OfferWithdrawnNotice);
        }
        finally
        {
            _busy = false;
        }
    }

    private void Notify(OfferResult result, LocalizedText done)
    {
        _notice = result switch
        {
            OfferResult.Done => done,
            OfferResult.Gone => Copy.OfferGone,
            _ => Copy.OfferFailed,
        };
        _noticeIsError = result == OfferResult.Failed;
    }
}
