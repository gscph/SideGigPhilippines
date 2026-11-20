using Microsoft.AspNetCore.Components;
using SideGigPh.Admin.Services;

namespace SideGigPh.Admin.Components.Auth.Views;

/// <summary>Signed in, but <c>GET ops/me</c> said no (or failed): the uid to add to <c>AdminUids</c>, Try again, Sign out.</summary>
public partial class NotOperatorPanel
{
    [Parameter, EditorRequired] public AdminUser User { get; set; } = default!;
    [Parameter] public string? Error { get; set; }
    [Parameter] public EventCallback OnRetry { get; set; }
    [Parameter] public EventCallback OnSignOut { get; set; }
}
