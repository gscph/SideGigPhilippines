using Microsoft.AspNetCore.Components;
using MudBlazor;
using SideGigPh.Admin.Components.Shared;
using SideGigPh.Admin.Extensions;
using SideGigPh.Admin.Services;
using SideGigPh.Contracts;

namespace SideGigPh.Admin.Components.User.Views;

/// <summary>
/// Every account, newest first: who they are (masked), which side, when they joined and were last seen. Paged, sorted
/// and searched on the server; the search and filter stay in the address (<c>?search=&amp;role=</c>) so a view can be shared.
/// </summary>
public partial class UserListView
{
    [Inject] public required UserService Users { get; set; }
    [Inject] public required NavigationManager Navigation { get; set; }

    /// <summary>The search the page opened with (<c>?search=</c>).</summary>
    [Parameter] public string? Search { get; set; }

    /// <summary>The side the page opened with (<c>?role=</c>).</summary>
    [Parameter] public string? Role { get; set; }

    private MudDataGrid<AdminUserDto> _grid = default!;
    private string? _search;
    private string _role = "";
    private int? _total;
    private string? _error;
    private DateTimeOffset _now = DateTimeOffset.UtcNow;

    protected override void OnInitialized()
    {
        _search = Search;
        _role = Role ?? "";
    }

    private async Task<GridData<AdminUserDto>> LoadAsync(GridState<AdminUserDto> state, CancellationToken cancellationToken)
    {
        _error = null;
        _now = DateTimeOffset.UtcNow;
        try
        {
            var data = await Users.GetUsersAsync(state, _search, _role, cancellationToken);
            _total = data.TotalItems;
            return data;
        }
        catch (AdminApiException ex)
        {
            _error = ex.Message;
            _total = null;
            return ListPaging.Empty<AdminUserDto>();
        }
    }

    private Task OnSearchAsync(string search)
    {
        _search = search;
        return ReloadAsync();
    }

    private Task OnRoleAsync(string role)
    {
        _role = role;
        return ReloadAsync();
    }

    private Task ReloadAsync()
    {
        Navigation.KeepInAddress(("search", _search), ("role", _role));
        return _grid.ReloadServerData();
    }

    private static Color RoleColor(AdminUserDto user) =>
        user.HasListing || user.Role == Wire.Professional ? Color.Primary
        : user.Role == Wire.Seeker ? Color.Dark
        : Color.Default;
}
