using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.JobPosts;

/// <summary>
/// One customer request on its own page (<see cref="JobPostRoutes.Request"/>), laid out like an Airbnb listing: the
/// service as a centered title, the place and distance, a stat row (budget, the customer's stars when they have any,
/// how long ago), then highlight rows (a service the worker offers, what the customer wrote, the worker's note) and
/// the photos. The bottom bar (<see cref="RequestActionBar"/>) takes the budget or offers another price
/// (<see cref="OfferBox"/>), and once offered shows the worker's price with Change and Take back. The request comes
/// from what <see cref="JobBoard"/> has loaded (the feed, else the worker's own offers); a link to one not loaded reads
/// them again, and one still missing says it is no longer open. The flag reports a fake request.
/// </summary>
public partial class RequestDetailPage : ComponentBase
{
    [Inject] private JobBoard Board { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;

    [Parameter] public long Id { get; set; }

    private RequestItem? _item;
    private bool _loaded;
    private bool _busy;
    private bool _offering;
    private bool _reported;
    private LocalizedText? _notice;
    private bool _noticeIsError;

    protected override async Task OnParametersSetAsync()
    {
        if (_loaded && _item?.Post.Id == Id)
        {
            return;
        }

        (_loaded, _offering, _reported, _notice) = (false, false, false, null);
        _item = await Board.FindRequestAsync(Id);
        _loaded = true;
    }

    private void OpenOfferBox() => _offering = true;

    private void CloseOfferBox() => _offering = false;

    private Task TakeBudgetAsync() => SendAsync(_item?.Post.BudgetPesos ?? 0, null);

    private Task SendTypedAsync((int Price, string? Note) offer) => SendAsync(offer.Price, offer.Note);

    private async Task SendAsync(int pricePesos, string? note)
    {
        if (_busy || _item is not { } item)
        {
            return; // a second tap before the button re-rendered
        }

        _busy = true;
        try
        {
            var result = await Board.SendOfferAsync(item.Post, pricePesos, note);
            if (result == OfferResult.Done)
            {
                _item = item.Offered(pricePesos, note);
                _offering = false;
            }

            Notify(result, Copy.OfferSentNotice);
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task WithdrawAsync()
    {
        if (_busy || _item is not { } item)
        {
            return; // a second tap before the button re-rendered
        }

        _busy = true;
        try
        {
            var result = await Board.WithdrawOfferAsync(item.Post);
            if (result == OfferResult.Done)
            {
                _item = item.Withdrawn();
            }

            Notify(result, Copy.OfferWithdrawnNotice);
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task ReportAsync()
    {
        if (_busy || _item is not { } item)
        {
            return; // a second tap before the button re-rendered
        }

        _busy = true;
        try
        {
            _reported = await Board.ReportAsync(item.Post);
            _notice = _reported ? Copy.PostReported : Copy.PostReportFailed;
            _noticeIsError = !_reported;
        }
        finally
        {
            _busy = false;
        }
    }

    private void Notify(OfferResult result, LocalizedText done)
    {
        if (result == OfferResult.Gone)
        {
            _item = null; // closed meanwhile: the page says it is no longer open
        }

        _notice = result switch
        {
            OfferResult.Done => done,
            OfferResult.Gone => Copy.OfferGone,
            _ => Copy.OfferFailed,
        };
        _noticeIsError = result == OfferResult.Failed;
    }
}
