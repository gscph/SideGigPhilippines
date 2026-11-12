using Microsoft.AspNetCore.Components;
using MobileAppTheme.Localization;
using MobileAppTheme.Services;

namespace MobileAppTheme.Components.Shared;

/// <summary>
/// The Me page frame every side of the app shares: the page title, the side's own sections
/// (<see cref="ChildContent"/>), then what every user has: the app language, a note that it's all
/// on this phone, and Start over, which deletes everything (<see cref="IAppAccount.StartOverAsync"/>)
/// and sends them back to <c>/</c>.
/// </summary>
public partial class MePage : ComponentBase
{
    [Inject] private IAppAccount Account { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;
    [Inject] private BusyIndicator Busy { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    [Parameter, EditorRequired] public string Title { get; set; } = string.Empty;

    /// <summary>The side's own sections, e.g. a profile photo or the name shown to others.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>What Start over deletes, in the app's words; defaults to a generic "everything on this phone".</summary>
    [Parameter] public string? StartOverWarning { get; set; }

    private bool _busy;

    private async Task StartOverAsync()
    {
        _busy = true;
        try
        {
            await Busy.RunAsync(Account.StartOverAsync);
        }
        finally
        {
            _busy = false;
        }

        Nav.NavigateTo("/", replace: true);
    }
}
