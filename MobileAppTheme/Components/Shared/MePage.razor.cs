using Microsoft.AspNetCore.Components;
using MobileAppTheme.Branding;
using MobileAppTheme.Localization;
using MobileAppTheme.Services;

namespace MobileAppTheme.Components.Shared;

/// <summary>
/// The Me page frame every side of the app shares: the page title, the side's own sections
/// (<see cref="ChildContent"/>), then what every user has: the app language, the legal pages
/// (<see cref="AppBrand.Legal"/>), Start over, which deletes what they made and starts the app again
/// (<see cref="IAppAccount.StartOverAsync"/>), and Delete account (<see cref="IAppAccount.DeleteAccountAsync"/>).
/// Both send them back to <c>/</c>.
/// </summary>
public partial class MePage : ComponentBase
{
    [Inject] private IAppAccount Account { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;
    [Inject] private BusyIndicator Busy { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;
    [Inject] private AppBrand Brand { get; set; } = default!;

    [Parameter, EditorRequired] public string Title { get; set; } = string.Empty;

    /// <summary>The side's own sections, e.g. a profile photo or the name shown to others.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>What Start over deletes, in the app's words; defaults to a generic "everything on this phone".</summary>
    [Parameter] public string? StartOverWarning { get; set; }

    private bool _busy;
    private string? _deleteError;
    private string? _startOverError;

    private async Task StartOverAsync()
    {
        if (_busy)
        {
            return; // a second tap before the buttons re-rendered as disabled
        }

        _busy = true;
        _startOverError = null;
        try
        {
            await Busy.RunAsync(Account.StartOverAsync);
        }
        catch (Exception)
        {
            // The adapters logged it (and it goes into the error report); the person can try again.
            _startOverError = T.Get(CommonCopy.StartOverFailed);
            return;
        }
        finally
        {
            _busy = false;
        }

        Nav.NavigateTo("/", replace: true);
    }

    private async Task DeleteAccountAsync()
    {
        if (_busy)
        {
            return;
        }

        _busy = true;
        _deleteError = null;
        try
        {
            await Busy.RunAsync(Account.DeleteAccountAsync);
        }
        catch (Exception)
        {
            // The account adapter logged it; the person can try again.
            _deleteError = T.Get(CommonCopy.DeleteAccountFailed);
            return;
        }
        finally
        {
            _busy = false;
        }

        Nav.NavigateTo("/", replace: true);
    }
}
