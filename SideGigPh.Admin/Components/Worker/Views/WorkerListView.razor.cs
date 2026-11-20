using Microsoft.AspNetCore.Components;
using MudBlazor;
using SideGigPh.Admin.Components.Shared;
using SideGigPh.Admin.Extensions;
using SideGigPh.Admin.Services;
using SideGigPh.Contracts;

namespace SideGigPh.Admin.Components.Worker.Views;

/// <summary>
/// The skilled workers, newest first: live or not, their trade and towns, price, photos, badges (and how many wait),
/// stars, chats, offers and jobs done. Rating and review counts include blind reviews the public can't see yet. Search
/// and filter (live, not live or a trade) stay in the address (<c>?search=&amp;filter=</c>).
/// </summary>
public partial class WorkerListView
{
    [Inject] public required WorkerService Workers { get; set; }
    [Inject] public required ReferenceService Reference { get; set; }
    [Inject] public required NavigationManager Navigation { get; set; }

    [Parameter] public string? Search { get; set; }
    [Parameter] public string? Filter { get; set; }

    private static readonly FilterChoice[] Always =
    [
        new("", "All workers"),
        new(WorkerService.Live, "Live listings"),
        new(WorkerService.NotLive, "Not live yet"),
    ];

    private MudDataGrid<AdminWorkerDto> _grid = default!;
    private string? _search;
    private string _filter = "";
    private IReadOnlyList<FilterChoice> _filters = Always;
    private IReadOnlyDictionary<string, string> _experience = new Dictionary<string, string>();
    private int? _total;
    private string? _error;
    private DateTimeOffset _now = DateTimeOffset.UtcNow;

    protected override async Task OnInitializedAsync()
    {
        _search = Search;
        _filter = Filter ?? "";
        _filters = [.. Always, .. (await Reference.TradesAsync()).Select(t => new FilterChoice(t.Id, t.English))];
        _experience = await Reference.ExperienceAsync();
    }

    private async Task<GridData<AdminWorkerDto>> LoadAsync(GridState<AdminWorkerDto> state, CancellationToken cancellationToken)
    {
        _error = null;
        _now = DateTimeOffset.UtcNow;
        try
        {
            var data = await Workers.GetWorkersAsync(state, _search, _filter, cancellationToken);
            _total = data.TotalItems;
            return data;
        }
        catch (AdminApiException ex)
        {
            _error = ex.Message;
            _total = null;
            return ListPaging.Empty<AdminWorkerDto>();
        }
    }

    private string ExperienceLabel(string? id) => id is null ? "—" : _experience.GetValueOrDefault(id, id);

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
