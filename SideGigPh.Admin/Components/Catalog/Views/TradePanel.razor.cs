using Microsoft.AspNetCore.Components;
using MudBlazor;
using SideGigPh.Contracts;

namespace SideGigPh.Admin.Components.Catalog.Views;

/// <summary>
/// One trade: its names, how many workers and open posts are in it, and its jobs in a table. Rename and add open a
/// dialog that saves itself; retire asks once more and says who is in it; restore is one click. The "Others" job can
/// be renamed but never retired. Changes run through <see cref="OnChange"/>, which shows any failure on the page.
/// </summary>
public partial class TradePanel
{
    [Inject] public required CatalogService Catalog { get; set; }
    [Inject] public required IDialogService Dialogs { get; set; }

    [Parameter, EditorRequired] public AdminTradeDto Trade { get; set; } = default!;
    [Parameter] public bool Busy { get; set; }
    [Parameter] public EventCallback<Func<Task>> OnChange { get; set; }

    private int ActiveJobs => Trade.Services.Count(s => s.IsActive);

    private async Task RenameAsync()
    {
        var parameters = new DialogParameters<TradeDialog>
        {
            { d => d.Trade, Trade },
            { d => d.Save, request => Catalog.UpdateTradeAsync(Trade.Id, request) },
        };
        await (await Dialogs.ShowAsync<TradeDialog>($"Rename {Trade.English}", parameters, TradeDialog.Options)).Result;
    }

    private async Task AddJobAsync()
    {
        var parameters = new DialogParameters<ServiceDialog>
        {
            { d => d.CategoryId, Trade.Id },
            { d => d.Save, request => Catalog.AddServiceAsync(request) },
        };
        await (await Dialogs.ShowAsync<ServiceDialog>($"Add a job to {Trade.English}", parameters, ServiceDialog.Options)).Result;
    }

    private async Task EditJobAsync(AdminServiceDto service)
    {
        var parameters = new DialogParameters<ServiceDialog>
        {
            { d => d.CategoryId, Trade.Id },
            { d => d.Service, service },
            { d => d.Save, request => Catalog.UpdateServiceAsync(service.Id, request) },
        };
        await (await Dialogs.ShowAsync<ServiceDialog>($"Rename {service.English}", parameters, ServiceDialog.Options)).Result;
    }

    private async Task RetireAsync()
    {
        if (await Dialogs.ShowMessageBoxAsync($"Retire {Trade.English}?", CatalogService.RetireTradeWarning(Trade),
                yesText: "Retire", cancelText: "Cancel") == true)
        {
            await OnChange.InvokeAsync(() => Catalog.SetTradeActiveAsync(Trade.Id, active: false));
        }
    }

    private Task RestoreAsync() => OnChange.InvokeAsync(() => Catalog.SetTradeActiveAsync(Trade.Id, active: true));

    private async Task RetireJobAsync(AdminServiceDto service)
    {
        if (await Dialogs.ShowMessageBoxAsync($"Retire {service.English}?", CatalogService.RetireServiceWarning(service),
                yesText: "Retire", cancelText: "Cancel") == true)
        {
            await OnChange.InvokeAsync(() => Catalog.SetServiceActiveAsync(service.Id, active: false));
        }
    }

    private Task RestoreJobAsync(AdminServiceDto service) => OnChange.InvokeAsync(() => Catalog.SetServiceActiveAsync(service.Id, active: true));
}
