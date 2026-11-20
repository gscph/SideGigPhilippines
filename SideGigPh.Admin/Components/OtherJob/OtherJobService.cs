using MudBlazor;
using SideGigPh.Admin.Components.Shared;
using SideGigPh.Admin.Services;
using SideGigPh.Contracts;

namespace SideGigPh.Admin.Components.OtherJob;

/// <summary>
/// What people typed after picking "Others" (<c>GET ops/other-jobs</c>): work we don't list yet, from households and
/// skilled workers, under a trade's "Others" job or as their own trade. Shows which trades and jobs to add next.
/// </summary>
public sealed class OtherJobService(AdminApi api)
{
    /// <summary>The grid's column → the API's sort word (only the date sorts; the API orders by when it was typed).</summary>
    public static readonly IReadOnlyDictionary<string, string> Sorts = new Dictionary<string, string>
    {
        [nameof(AdminOtherJobDto.CreatedAt)] = AdminLists.SortJoined,
    };

    /// <summary>The filter's choices that are always there: everyone, one side, or the "Others" trade itself; the trades follow.</summary>
    public static readonly IReadOnlyList<FilterChoice> Always =
    [
        new("", "Everyone"),
        new(Wire.Seeker, "Households"),
        new(Wire.Professional, "Skilled workers"),
        new(OtherWork.TradeId, "Typed as their own trade"),
    ];

    /// <summary>A page; <paramref name="filter"/> is a side, a trade id or empty for all.</summary>
    public async Task<GridData<AdminOtherJobDto>> GetNotesAsync(GridState<AdminOtherJobDto> state, string? search, string? filter, CancellationToken cancellationToken)
    {
        var page = await api.GetAsync($"ops/other-jobs?{state.ToAdminQuery(search, Sorts, ("filter", filter))}", ApiJson.Default.AdminOtherJobPageDto, cancellationToken);
        return page.Items.ToGridData(page.Total);
    }

    /// <summary>The Groups tab's time choices: the last N days, 0 = all of it.</summary>
    public static readonly IReadOnlyList<(int Days, string Label)> Periods =
    [
        (0, "All time"),
        (7, "Last 7 days"),
        (30, "Last 30 days"),
        (90, "Last 90 days"),
        (365, "Last year"),
    ];

    /// <summary>
    /// The notes grouped (<c>GET ops/other-jobs/groups</c>), biggest first: the same text after cleanup, and the same
    /// meaning when the API's embeddings model is set up. <paramref name="days"/> 0 = all time.
    /// </summary>
    public Task<AdminOtherJobGroupsDto> GetGroupsAsync(string? filter, int days, CancellationToken cancellationToken)
    {
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(filter))
        {
            query.Add($"filter={Uri.EscapeDataString(filter)}");
        }

        if (days > 0)
        {
            query.Add($"days={days}");
        }

        return api.GetAsync($"ops/other-jobs/groups?{string.Join('&', query)}", ApiJson.Default.AdminOtherJobGroupsDto, cancellationToken);
    }

    /// <summary>"Household" or "Skilled worker".</summary>
    public static string SideLabel(string side) => side == Wire.Professional ? "Skilled worker" : "Household";
}
