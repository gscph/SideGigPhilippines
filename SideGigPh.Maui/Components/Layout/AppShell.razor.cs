using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace SideGigPh.Maui.Components.Layout;

/// <summary>
/// The signed-in app frame shared by Professionals and Seekers, Slack style: brand bar with the
/// user's avatar on top, the page in the middle, icon tabs at the bottom. Each side's layout
/// (<c>ProLayout</c>, <c>SeekerLayout</c>) supplies its own tabs and user; the frame is the same.
/// </summary>
public partial class AppShell : ComponentBase, IDisposable
{
    [Inject] private NavigationManager Nav { get; set; } = default!;

    [Parameter, EditorRequired] public IReadOnlyList<AppTab> Tabs { get; set; } = [];

    /// <summary>Where the avatar in the top bar goes (the user's profile or account page).</summary>
    [Parameter, EditorRequired] public string ProfileHref { get; set; } = string.Empty;

    /// <summary>The user's name for the avatar initials; empty shows a person icon.</summary>
    [Parameter] public string? UserName { get; set; }

    [Parameter] public string? UserPhotoUrl { get; set; }

    /// <summary>A green dot on the avatar, e.g. a Professional whose listing is published.</summary>
    [Parameter] public bool ShowStatusDot { get; set; }

    [Parameter] public RenderFragment? ChildContent { get; set; }

    private string _path = string.Empty;

    protected override void OnInitialized()
    {
        _path = CurrentPath();
        Nav.LocationChanged += OnLocationChanged;
    }

    public void Dispose() => Nav.LocationChanged -= OnLocationChanged;

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        _path = CurrentPath();
        InvokeAsync(StateHasChanged);
    }

    private string CurrentPath() => "/" + Nav.ToBaseRelativePath(Nav.Uri).Split('?', '#')[0].TrimEnd('/');
}
