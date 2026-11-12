using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Professional;

/// <summary>
/// A one-line purple strip under the top bar while the listing isn't published: customers can't see the worker
/// yet. Tapping it opens Pro home, where the "Ihanda ang listing mo" checklist and the Publish button are.
/// Always one line (long text is cut with "…").
/// </summary>
public partial class NotLiveBanner : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    private void GoToSetup() => Nav.NavigateTo(ProRoutes.Home);
}
