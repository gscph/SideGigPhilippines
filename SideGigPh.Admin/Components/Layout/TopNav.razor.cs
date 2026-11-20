using Microsoft.AspNetCore.Components;

namespace SideGigPh.Admin.Components.Layout;

/// <summary>The top bar: the menu button (opens and closes <see cref="SideNav"/>), the name, and who is signed in.</summary>
public partial class TopNav
{
    [Parameter] public EventCallback OnMenu { get; set; }
}
