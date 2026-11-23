using Microsoft.AspNetCore.Components;

namespace SideGigPh.Admin.Components.Layout;

/// <summary>The side menu: the dashboard, the lists of people, the trades and jobs the app offers, and the operator's to-dos (badges, error reports).</summary>
public partial class SideNav
{
    [Parameter] public bool Open { get; set; }
    [Parameter] public EventCallback<bool> OpenChanged { get; set; }
}
