using Microsoft.AspNetCore.Components;
using MudBlazor;
using SideGigPh.Admin.Components.Shared;
using SideGigPh.Admin.Extensions;
using SideGigPh.Admin.Services;
using SideGigPh.Contracts;

namespace SideGigPh.Admin.Components.Seeker.Views;

/// <summary>
/// The households, newest first, with what each did: posts (open), chats, jobs done, reviews written. The town is the
/// one of their latest post or chat (the app keeps a household's home town on the phone only). Search and town stay in
/// the address (<c>?search=&amp;town=</c>).
/// </summary>
public partial class SeekerListView
{
    [Inject] public required SeekerService Seekers { get; set; }
    [Inject] public required ReferenceService Reference { get; set; }
    [Inject] public required NavigationManager Navigation { get; set; }

    [Parameter] public string? Search { get; set; }
    [Parameter] public string? Town { get; set; }

    private MudDataGrid<AdminSeekerDto> _grid = default!;
    private string? _search;
    private string _town = "";
    private IReadOnlyList<FilterChoice> _towns = [new("", "All towns")];
    private int? _total;
    private string? _error;
    private DateTimeOffset _now = DateTimeOffset.UtcNow;

    protected override async Task OnInitializedAsync()
    {
        _search = Search;
        _town = Town ?? "";
        _towns = [new("", "All towns"), .. (await Reference.TownsAsync()).Select(t => new FilterChoice(t.Id, t.English))];
    }

    private async Task<GridData<AdminSeekerDto>> LoadAsync(GridState<AdminSeekerDto> state, CancellationToken cancellationToken)
    {
        _error = null;
        _now = DateTimeOffset.UtcNow;
        try
        {
            var data = await Seekers.GetSeekersAsync(state, _search, _town, cancellationToken);
            _total = data.TotalItems;
            return data;
        }
        catch (AdminApiException ex)
        {
            _error = ex.Message;
            _total = null;
            return ListPaging.Empty<AdminSeekerDto>();
        }
    }

    private Task OnSearchAsync(string search)
    {
        _search = search;
        return ReloadAsync();
    }

    private Task OnTownAsync(string town)
    {
        _town = town;
        return ReloadAsync();
    }

    private Task ReloadAsync()
    {
        Navigation.KeepInAddress(("search", _search), ("town", _town));
        return _grid.ReloadServerData();
    }
}
