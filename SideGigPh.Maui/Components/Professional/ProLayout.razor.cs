using Microsoft.AspNetCore.Components;
using MudBlazor;
using SideGigPh.Maui.Components.Layout;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Professional;

/// <summary>
/// The Professional's pages inside the shared <see cref="AppShell"/>: tabs Home, Inbox (with a
/// "new" badge), Reviews and Photos; the avatar opens their profile, with a green dot once
/// published. Loads <see cref="ProWorkspace"/> first and sends anyone who hasn't finished the
/// Professional interview back to <c>/</c>.
/// </summary>
public partial class ProLayout : LayoutComponentBase, IDisposable
{
    [Inject] private ProWorkspace Workspace { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    private bool _ready;

    private IReadOnlyList<AppTab> Tabs =>
    [
        new(ProRoutes.Home, Icons.Material.Filled.Home, T.Get(Copy.NavHome)),
        new(ProRoutes.Inquiries, Icons.Material.Filled.ChatBubbleOutline, T.Get(Copy.NavInbox), Workspace.NewInquiryCount),
        new(ProRoutes.Reviews, Icons.Material.Filled.StarOutline, T.Get(Copy.NavReviews)),
        new(ProRoutes.Photos, Icons.Material.Filled.PhotoLibrary, T.Get(Copy.NavPhotos)),
    ];

    protected override async Task OnInitializedAsync()
    {
        Workspace.Changed += OnWorkspaceChanged;
        if (!await Workspace.EnsureLoadedAsync())
        {
            Nav.NavigateTo("/", replace: true);
            return;
        }

        _ready = true;
    }

    public void Dispose() => Workspace.Changed -= OnWorkspaceChanged;

    private void OnWorkspaceChanged() => InvokeAsync(StateHasChanged);
}
