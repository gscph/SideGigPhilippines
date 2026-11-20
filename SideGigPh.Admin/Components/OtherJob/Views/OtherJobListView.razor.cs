using Microsoft.AspNetCore.Components;
using MudBlazor;
using SideGigPh.Admin.Components.Shared;
using SideGigPh.Admin.Extensions;
using SideGigPh.Admin.Services;
using SideGigPh.Contracts;

namespace SideGigPh.Admin.Components.OtherJob.Views;

/// <summary>
/// What people typed under "Others", newest first: the text, the trade it was under (or "Own trade"), the side and who.
/// Search and filter (a side, the "Others" trade or a trade) stay in the address (<c>?search=&amp;filter=</c>).
/// </summary>
public partial class OtherJobListView
{
    [Inject] public required OtherJobService Notes { get; set; }
    [Inject] public required ReferenceService Reference { get; set; }
    [Inject] public required NavigationManager Navigation { get; set; }

    [Parameter] public string? Search { get; set; }
    [Parameter] public string? Filter { get; set; }

    private MudDataGrid<AdminOtherJobDto> _grid = default!;
    private string? _search;
    private string _filter = "";
    private IReadOnlyList<FilterChoice> _filters = OtherJobService.Always;
    private int? _total;
    private string? _error;

    protected override async Task OnInitializedAsync()
    {
        _search = Search;
        _filter = Filter ?? "";
        _filters = [.. OtherJobService.Always, .. (await Reference.TradesAsync()).Select(t => new FilterChoice(t.Id, t.English))];
    }

    private async Task<GridData<AdminOtherJobDto>> LoadAsync(GridState<AdminOtherJobDto> state, CancellationToken cancellationToken)
    {
        _error = null;
        try
        {
            var data = await Notes.GetNotesAsync(state, _search, _filter, cancellationToken);
            _total = data.TotalItems;
            return data;
        }
        catch (AdminApiException ex)
        {
            _error = ex.Message;
            _total = null;
            return ListPaging.Empty<AdminOtherJobDto>();
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
