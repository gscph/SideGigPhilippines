using Microsoft.AspNetCore.Components;
using MudBlazor;
using SideGigPh.Maui.Components.Layout;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Seeker;

/// <summary>
/// The Seeker's pages inside the shared <see cref="AppShell"/>: tabs Home (search results sit
/// under it), Inbox and Me (also the avatar; their reviews sit under it, and the badge counts
/// pros they contacted but haven't rated).
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
        new(SeekerRoutes.Home, Icons.Material.Filled.Home, T.Get(Copy.NavHome), MatchPrefix: true),
        new(SeekerRoutes.Inbox, Icons.Material.Filled.ChatBubbleOutline, T.Get(Copy.NavInbox), MatchPrefix: true),
        new(SeekerRoutes.Me, Icons.Material.Filled.Person, T.Get(Copy.NavMe), Workspace.ToRate.Count, MatchPrefix: true),
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
