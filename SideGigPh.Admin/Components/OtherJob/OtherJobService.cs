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

    /// <summary>"Household" or "Skilled worker".</summary>
    public static string SideLabel(string side) => side == Wire.Professional ? "Skilled worker" : "Household";
}
