using Microsoft.AspNetCore.Components;
using MudBlazor;
using SideGigPh.Maui.Components.Layout;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Seeker;

/// <summary>
/// The Seeker's pages inside the shared <see cref="AppShell"/>: tabs Home, Search, Inbox,
/// Reviews (badge = pros they contacted but haven't rated) and Me (also the avatar).
/// Loads <see cref="SeekerWorkspace"/> first and sends anyone who hasn't finished the Seeker
/// interview back to <c>/</c>.
/// </summary>
public partial class SeekerLayout : LayoutComponentBase, IDisposable
{
    [Inject] private SeekerWorkspace Workspace { get; set; } = default!;
    [Inject] private LocalAccount Account { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    private bool _ready;

    private IReadOnlyList<AppTab> Tabs =>
    [
        new(SeekerRoutes.Home, Icons.Material.Filled.Home, T.Get(Copy.NavHome)),
        new(SeekerRoutes.Search, Icons.Material.Filled.Search, T.Get(Copy.NavSearch)),
        new(SeekerRoutes.Inbox, Icons.Material.Filled.ChatBubbleOutline, T.Get(Copy.NavInbox), MatchPrefix: true),
        new(SeekerRoutes.Reviews, Icons.Material.Filled.StarOutline, T.Get(Copy.NavReviews), Workspace.ToRate.Count),
        new(SeekerRoutes.Me, Icons.Material.Filled.Person, T.Get(Copy.NavMe)),
    ];

    protected override async Task OnInitializedAsync()
    {
        Workspace.Changed += OnWorkspaceChanged;
        Account.Changed += OnWorkspaceChanged;
        if (!await Workspace.EnsureLoadedAsync())
        {
            Nav.NavigateTo("/", replace: true);
            return;
        }

        _ready = true;
    }

    public void Dispose()
    {
        Workspace.Changed -= OnWorkspaceChanged;
        Account.Changed -= OnWorkspaceChanged;
    }

    private void OnWorkspaceChanged() => InvokeAsync(StateHasChanged);
}
