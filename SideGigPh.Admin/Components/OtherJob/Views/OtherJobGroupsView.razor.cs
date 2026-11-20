using Microsoft.AspNetCore.Components;
using SideGigPh.Admin.Components.Shared;
using SideGigPh.Admin.Extensions;
using SideGigPh.Admin.Services;
using SideGigPh.Contracts;

namespace SideGigPh.Admin.Components.OtherJob.Views;

/// <summary>
/// What people typed under "Others", grouped and biggest first, so the most requested missing work is at the top:
/// each group's most typed text, its other spellings, how many notes, from which side and under which trades. Says
/// when AI grouping was off (exact matches only) or the notes were capped. Filter and period stay in the address
/// (<c>?filter=&amp;days=</c>).
/// </summary>
public partial class OtherJobGroupsView
{
    [Inject] public required OtherJobService Notes { get; set; }
    [Inject] public required ReferenceService Reference { get; set; }
    [Inject] public required NavigationManager Navigation { get; set; }

    [Parameter] public string? Filter { get; set; }
    [Parameter] public int? Days { get; set; }

    private string _filter = "";
    private int _days;
    private IReadOnlyList<FilterChoice> _filters = OtherJobService.Always;
    private AdminOtherJobGroupsDto? _groups;
    private bool _loading;
    private int _asked;
    private string? _error;

    protected override async Task OnInitializedAsync()
    {
        _filter = Filter ?? "";
        _days = OtherJobService.Periods.Any(p => p.Days == Days) ? Days!.Value : 0;
        _filters = [.. OtherJobService.Always, .. (await Reference.TradesAsync()).Select(t => new FilterChoice(t.Id, t.English))];
        await LoadAsync();
    }

    /// <summary>The group's other ways of writing it (its label left out), joined for one caption line.</summary>
    private static string OtherWays(AdminOtherJobGroupDto group) =>
        string.Join(" · ", group.Examples.Where(e => e != group.Label));

    private Task OnFilterAsync(string filter)
    {
        _filter = filter;
        return ReloadAsync();
    }

    private Task OnDaysAsync(int days)
    {
        _days = days;
        return ReloadAsync();
    }

    private Task ReloadAsync()
    {
        Navigation.KeepInAddress(("filter", _filter), ("days", _days > 0 ? _days.ToString() : null));
        return LoadAsync();
    }

    /// <summary>Loads the groups; when the filter changes while a load waits, only the newest answer is shown.</summary>
    private async Task LoadAsync()
    {
        var asked = ++_asked;
        _loading = true;
        _error = null;
        try
        {
            var groups = await Notes.GetGroupsAsync(_filter, _days, CancellationToken.None);
            if (asked == _asked)
            {
                _groups = groups;
            }
        }
        catch (AdminApiException ex)
        {
            if (asked == _asked)
            {
                _error = ex.Message;
                _groups = null;
            }
        }
        finally
        {
            if (asked == _asked)
            {
                _loading = false;
            }
        }
    }
}
