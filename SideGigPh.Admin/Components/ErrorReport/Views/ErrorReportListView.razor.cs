using Microsoft.AspNetCore.Components;
using MudBlazor;
using SideGigPh.Admin.Components.Shared;
using SideGigPh.Admin.Extensions;
using SideGigPh.Admin.Services;
using SideGigPh.Contracts;

namespace SideGigPh.Admin.Components.ErrorReport.Views;

/// <summary>
/// The error reports, newest first: kind, message, how many times, the page, the app version and the phone. A row opens
/// to the stack trace and the trail of pages before it (<see cref="ErrorReportDetail"/>). Search and filter (a kind or a
/// channel) stay in the address (<c>?search=&amp;filter=</c>).
/// </summary>
public partial class ErrorReportListView
{
    [Inject] public required ErrorReportService Reports { get; set; }
    [Inject] public required NavigationManager Navigation { get; set; }

    [Parameter] public string? Search { get; set; }
    [Parameter] public string? Filter { get; set; }

    private MudDataGrid<AdminErrorReportDto> _grid = default!;
    private string? _search;
    private string _filter = "";
    private int? _total;
    private string? _error;

    protected override void OnInitialized()
    {
        _search = Search;
        _filter = Filter ?? "";
    }

    private async Task<GridData<AdminErrorReportDto>> LoadAsync(GridState<AdminErrorReportDto> state, CancellationToken cancellationToken)
    {
        _error = null;
        try
        {
            var data = await Reports.GetReportsAsync(state, _search, _filter, cancellationToken);
            _total = data.TotalItems;
            return data;
        }
        catch (AdminApiException ex)
        {
            _error = ex.Message;
            _total = null;
            return ListPaging.Empty<AdminErrorReportDto>();
        }
    }

    private Task OnSearchAsync(string search)
    {
        _search = search;
        return ReloadAsync();
    }

    private Task OnFilterAsync(string filter)
    {
        _filter = filter;
        return ReloadAsync();
    }

    private Task ReloadAsync()
    {
        Navigation.KeepInAddress(("search", _search), ("filter", _filter));
        return _grid.ReloadServerData();
    }
}
