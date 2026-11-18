using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Components.Professional;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.JobPosts;

/// <summary>
/// One customer request on its own full-screen page (<see cref="JobPostRoutes.Request"/>), laid out like Airbnb's
/// "Review and continue": a close ✕ and the flag, a big title that says what to do ("Send your price", "Your offer"),
/// then one card (<see cref="RequestSummary"/>: picture, service, place, stars and time; then rows: the worker's price
/// with Change, the customer's budget, what they need with the photos). A new request asks for the price as two choices
/// (<see cref="PriceChoice"/>: take the budget, or another price with a note) and one Send offer button
/// (<see cref="RequestFooter"/>); a sent offer is changed in place (Cancel / Save) or taken back by the link under the
/// card. Outside the tab shell, so it loads <see cref="ProWorkspace"/> itself. The request comes from what
/// <see cref="JobBoard"/> has loaded (the feed, else the worker's own offers); one still missing says it is no longer
/// open. The flag reports a fake request.
/// </summary>
public partial class RequestDetailPage : ComponentBase
{
    [Inject] private ProWorkspace Workspace { get; set; } = default!;
    [Inject] private JobBoard Board { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    [Parameter] public long Id { get; set; }

    private RequestItem? _item;
    private bool _loaded;
    private bool _busy;
    private bool _editing;
    private bool _otherPrice;
    private int? _price;
    private string? _note;
    private bool _reported;
    private LocalizedText? _notice;
    private bool _noticeIsError;

    /// <summary>What Send offer / Save would send: the budget, or the typed price and note.</summary>
    private OfferDraft Draft => OfferDraft.Of(_item?.Post.BudgetPesos ?? 0, _otherPrice || _editing, _price, _note);

    private bool CanSend => Draft.IsValid;

    protected override async Task OnParametersSetAsync()
    {
        if (_loaded && _item?.Post.Id == Id)
        {
            return;
        }

        if (!await Workspace.EnsureLoadedAsync())
        {
            Nav.NavigateTo("/", replace: true);
            return;
        }

        (_loaded, _reported, _notice) = (false, false, null);
        _item = await Board.FindRequestAsync(Id);
        ResetPrice();
        _loaded = true;
    }

    private void Close() => Nav.NavigateTo(ProRoutes.Requests);

    private void StartEditing()
    {
        ResetPrice();
        _editing = true;
    }

    private void StopEditing()
    {
        ResetPrice();
        _editing = false;
    }

    /// <summary>The price fields start from the worker's offer, else the customer's budget; the choice from "take it".</summary>
    private void ResetPrice()
    {
        _otherPrice = false;
        _price = _item?.MyPricePesos ?? _item?.Post.BudgetPesos;
        _note = _item?.MyNote;
    }

    private async Task SendAsync()
    {
        if (_busy || _item is not { } item || Draft is not { IsValid: true } draft)
        {
            return; // a second tap before the button re-rendered
        }

        _busy = true;
        try
        {
            var result = await Board.SendOfferAsync(item.Post, draft.PricePesos, draft.Note);
            if (result == OfferResult.Done)
            {
                _item = item.Offered(draft.PricePesos, draft.Note);
                _editing = false;
                ResetPrice();
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
                ResetPrice();
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
