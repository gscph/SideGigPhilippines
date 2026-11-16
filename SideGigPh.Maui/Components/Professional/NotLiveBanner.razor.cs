using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Professional;

/// <summary>
/// A one-line purple strip under the top bar while the listing isn't published: customers can't see the worker
/// yet. Tapping it opens Pro home, where the "Ihanda ang listing mo" checklist and the Publish button are.
/// Always one line (long text is cut with "…"). When only the number is missing (someone who chose "Do this later" on
/// the add-number page, e.g. out of SMS codes for today) it says so: confirm the number to publish.
/// </summary>
public partial class NotLiveBanner : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    /// <summary>Photos and references are done; only the confirmed number stands between them and Publish.</summary>
    [Parameter] public bool OnlyNumberLeft { get; set; }

    private void GoToSetup() => Nav.NavigateTo(ProRoutes.Home);
}
