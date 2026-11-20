using MudBlazor;
using SideGigPh.Admin.Services;
using SideGigPh.Contracts;

namespace SideGigPh.Admin.Components.Seeker;

/// <summary>The households (<c>GET admin/seekers</c>) and what each did: posts, chats, completed jobs, reviews.</summary>
public sealed class SeekerService(AdminApi api)
{
    /// <summary>The grid's column → the API's sort word.</summary>
    public static readonly IReadOnlyDictionary<string, string> Sorts = new Dictionary<string, string>
    {
        [nameof(AdminSeekerDto.CreatedAt)] = AdminLists.SortJoined,
        [nameof(AdminSeekerDto.LastActiveAt)] = AdminLists.SortActive,
        [nameof(AdminSeekerDto.Name)] = AdminLists.SortName,
        [nameof(AdminSeekerDto.Posts)] = AdminLists.SortPosts,
        [nameof(AdminSeekerDto.CompletedJobs)] = AdminLists.SortCompleted,
    };

    /// <summary>A page; <paramref name="townId"/> keeps the households whose latest post or chat was in that town.</summary>
    public async Task<GridData<AdminSeekerDto>> GetSeekersAsync(GridState<AdminSeekerDto> state, string? search, string? townId, CancellationToken cancellationToken)
    {
        var page = await api.GetAsync($"admin/seekers?{state.ToAdminQuery(search, Sorts, ("town", townId))}", ApiJson.Default.AdminSeekerPageDto, cancellationToken);
        return page.Items.ToGridData(page.Total);
    }
}
