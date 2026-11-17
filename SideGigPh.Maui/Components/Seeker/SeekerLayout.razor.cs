using Microsoft.AspNetCore.Components;
using MudBlazor;
using SideGigPh.Maui.Components.Layout;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Seeker;

/// <summary>
/// The Seeker's pages inside the shared <see cref="AppShell"/>: tabs Home (search results sit
/// under it), Post (what they need done, shown to workers nearby), Inbox and Me (also the avatar; their reviews sit under it, and the badge counts
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
    [Inject] private ChatAlerts Alerts { get; set; } = default!;
    [Inject] private JobBoard Board { get; set; } = default!;

    private bool _ready;
    private IDisposable? _alerts;

    private IReadOnlyList<AppTab> Tabs =>
    [
        new(SeekerRoutes.Home, Icons.Material.Filled.Home, T.Get(Copy.NavHome), MatchPrefix: true),
        new(SeekerRoutes.Posts, Icons.Material.Filled.AddCircleOutline, T.Get(Copy.NavPosts), MatchPrefix: true),
        new(SeekerRoutes.Inbox, Icons.Material.Filled.ChatBubbleOutline, T.Get(Copy.NavInbox), Workspace.UnreadCount, MatchPrefix: true),
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
        // A push is a new message or a new offer on one of their posts: both lists are read again.
        _alerts = Alerts.Attach(() => Task.WhenAll(Workspace.RefreshActivityAsync(), Board.LoadMineAsync()), path => InvokeAsync(() => Nav.NavigateTo(path)));
        Alerts.ShowUnread(Workspace.UnreadChatIds);

        // Seekers are asked only after they write to a worker (ThreadPage); here the phone is just registered.
        _ = Alerts.RegisterAsync();
    }

    public void Dispose()
    {
        Workspace.Changed -= OnWorkspaceChanged;
        Account.Changed -= OnWorkspaceChanged;
        _alerts?.Dispose();
    }

    private void OnWorkspaceChanged()
    {
        if (_ready)
        {
            Alerts.ShowUnread(Workspace.UnreadChatIds);
        }

        InvokeAsync(StateHasChanged);
    }
}
