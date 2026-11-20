using Microsoft.AspNetCore.Components;

namespace SideGigPh.Admin.Components.Shared;

/// <summary>Every page's title (the browser tab too, and the h1 the router focuses), a line under it, and actions on the right.</summary>
public partial class PageHeader
{
    [Parameter, EditorRequired] public string Title { get; set; } = "";
    [Parameter] public string? SubTitle { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }
}
