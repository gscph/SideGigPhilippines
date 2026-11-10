using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Shared;

/// <summary>
/// The Me page both sides share: the page title, the side's own sections (<see cref="ChildContent"/>),
/// then what every user has: the app language, a note that it's all on this phone, and Start over,
/// which deletes everything and sends them back to the first onboarding screen.
/// </summary>
public partial class MePage : ComponentBase
{
    [Inject] private LocalAccount Account { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    [Parameter, EditorRequired] public string Title { get; set; } = string.Empty;

    /// <summary>The side's own sections, e.g. the pro's photo and listing, or the Seeker's name.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    private bool _busy;

    private async Task StartOverAsync()
    {
        _busy = true;
        try
        {
            await Account.StartOverAsync();
        }
        finally
        {
            _busy = false;
        }

        Nav.NavigateTo("/", replace: true);
    }
}
