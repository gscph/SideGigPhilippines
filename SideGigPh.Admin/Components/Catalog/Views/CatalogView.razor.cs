using Microsoft.AspNetCore.Components;
using MudBlazor;
using SideGigPh.Admin.Services;
using SideGigPh.Contracts;

namespace SideGigPh.Admin.Components.Catalog.Views;

/// <summary>
/// Every trade (offered ones first, in the app's order, then retired ones), each a panel with its jobs. Adding a trade
/// opens <see cref="TradeDialog"/>; each panel's own buttons go through <see cref="RunAsync"/>, one change at a time.
/// </summary>
public partial class CatalogView
{
    [Inject] public required CatalogService Catalog { get; set; }
    [Inject] public required IDialogService Dialogs { get; set; }

    private bool _loading = true;
    private bool _busy;
    private string? _error;

    private int ActiveCount => Catalog.Current?.Trades.Count(t => t.IsActive) ?? 0;

    private int RetiredCount => Catalog.Current?.Trades.Count(t => !t.IsActive) ?? 0;

    protected override Task OnInitializedAsync() => LoadAsync();

    private async Task LoadAsync()
    {
        if (_busy) return;
        _busy = true;
        _loading = Catalog.Current is null;
        _error = null;
        try
        {
            await Catalog.LoadAsync();
        }
        catch (AdminApiException ex)
        {
            _error = ex.Message;
        }
        finally
        {
            _busy = false;
            _loading = false;
        }
    }

    private async Task AddTradeAsync()
    {
        if (_busy) return;
        var parameters = new DialogParameters<TradeDialog>
        {
            { d => d.Save, request => Catalog.AddTradeAsync(request) },
        };
        await (await Dialogs.ShowAsync<TradeDialog>("Add a trade", parameters, TradeDialog.Options)).Result;
    }

    /// <summary>Runs one change from a trade's panel (retire, restore…), showing what went wrong here.</summary>
    private async Task RunAsync(Func<Task> change)
    {
        if (_busy) return;
        _busy = true;
        _error = null;
        try
        {
            await change();
        }
        catch (AdminApiException ex)
        {
            _error = ex.Message;
        }
        finally
        {
            _busy = false;
        }
    }
}
