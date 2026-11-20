using Microsoft.AspNetCore.Components;

namespace SideGigPh.Admin.Components.Auth.Views;

/// <summary>The signed-out screen: one Google button and why the last try failed.</summary>
public partial class SignInPanel
{
    [Parameter] public EventCallback OnSignIn { get; set; }
    [Parameter] public bool Busy { get; set; }
    [Parameter] public string? Error { get; set; }
}
