using MudBlazor;
using SideGigPh.Admin.Components.Shared;
using SideGigPh.Admin.Services;
using SideGigPh.Contracts;

namespace SideGigPh.Admin.Components.User;

/// <summary>Every account (<c>GET ops/users</c>), a page at a time, sorted and searched on the server.</summary>
public sealed class UserService(AdminApi api)
{
    /// <summary>The grid's column → the API's sort word.</summary>
    public static readonly IReadOnlyDictionary<string, string> Sorts = new Dictionary<string, string>
    {
        [nameof(AdminUserDto.CreatedAt)] = AdminLists.SortJoined,
        [nameof(AdminUserDto.LastActiveAt)] = AdminLists.SortActive,
        [nameof(AdminUserDto.Name)] = AdminLists.SortName,
    };

    /// <summary>The role filter's choices: the API's <c>role</c> values with a label; empty = everyone.</summary>
    public static readonly IReadOnlyList<FilterChoice> Roles =
    [
        new("", "Everyone"),
        new(Wire.Seeker, "Households"),
        new(Wire.Professional, "Skilled workers"),
        new(AdminLists.NoRole, "No side picked yet"),
    ];

    public async Task<GridData<AdminUserDto>> GetUsersAsync(GridState<AdminUserDto> state, string? search, string? role, CancellationToken cancellationToken)
    {
        var page = await api.GetAsync($"ops/users?{state.ToAdminQuery(search, Sorts, ("role", role))}", ApiJson.Default.AdminUserPageDto, cancellationToken);
        return page.Items.ToGridData(page.Total);
    }

    /// <summary>"Household", "Skilled worker" or "No side yet".</summary>
    public static string RoleLabel(AdminUserDto user) =>
        user.HasListing || user.Role == Wire.Professional ? "Skilled worker"
        : user.Role == Wire.Seeker ? "Household"
        : "No side yet";
}
