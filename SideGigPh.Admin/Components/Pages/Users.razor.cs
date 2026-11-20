using Microsoft.AspNetCore.Components;

namespace SideGigPh.Admin.Components.Pages;

/// <summary>Every account (<see cref="User.Views.UserListView"/>), opened with <c>?search=&amp;role=</c> when given.</summary>
public partial class Users
{
    [SupplyParameterFromQuery] public string? Search { get; set; }
    [SupplyParameterFromQuery] public string? Role { get; set; }
}
