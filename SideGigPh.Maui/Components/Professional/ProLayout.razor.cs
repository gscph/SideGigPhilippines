using Microsoft.AspNetCore.Components;
using MudBlazor;
using SideGigPh.Maui.Components.Layout;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Professional;

/// <summary>
/// The Professional's pages inside the shared <see cref="AppShell"/>: tabs Home, Inbox (with an
/// unread badge), Reviews and Me (profile, work photos, language, Start over); the avatar opens
/// Me too, with a green dot once published; until then a <see cref="NotLiveBanner"/> sits under the top bar. Loads <see cref="ProWorkspace"/> first and sends anyone who hasn't finished the
/// Professional interview back to <c>/</c>.
/// </summary>
public partial class ProLayout : LayoutComponentBase, IDisposable
{
    [Inject] private ProWorkspace Workspace { get; set; } = default!;
    [Inject] private LocalAccount Account { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    private bool _ready;

    /// <summary>Under the top bar until the listing is published: customers can't see the worker yet.</summary>
    private static readonly RenderFragment NotLive = builder =>
    {
        builder.OpenComponent<NotLiveBanner>(0);
        builder.CloseComponent();
    };

    private IReadOnlyList<AppTab> Tabs =>
    [
        new(ProRoutes.Home, Icons.Material.Filled.Home, T.Get(Copy.NavHome)),
        new(ProRoutes.Inbox, Icons.Material.Filled.ChatBubbleOutline, T.Get(Copy.NavInbox), Workspace.UnreadCount, MatchPrefix: true),
        new(ProRoutes.Reviews, Icons.Material.Filled.StarOutline, T.Get(Copy.NavReviews)),
        new(ProRoutes.Me, Icons.Material.Filled.Person, T.Get(Copy.NavMe), MatchPrefix: true),
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
