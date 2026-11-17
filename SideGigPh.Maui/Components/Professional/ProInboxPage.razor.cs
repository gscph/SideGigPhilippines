using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Professional;

/// <summary>
/// The pro's Inbox: one chat per customer who messaged them, most recent message first. Redraws when the workspace
/// changes (a push re-read the Inbox while it is open).
/// </summary>
public partial class ProInboxPage : ComponentBase, IDisposable
{
    [Inject] private ProWorkspace Workspace { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;

    protected override Task OnInitializedAsync()
    {
        Workspace.Changed += OnChanged;
        return Workspace.RefreshIfStaleAsync();
    }

    public void Dispose() => Workspace.Changed -= OnChanged;

    private void OnChanged() => _ = InvokeAsync(StateHasChanged);
}
