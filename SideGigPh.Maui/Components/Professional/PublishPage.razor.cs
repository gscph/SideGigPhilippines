using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Professional;

/// <summary>
/// Going live: the pro sees their card one last time and takes a short pledge (answer
/// customers, honest work at the posted price) before publishing. A focused full-screen
/// step, so it uses the interview look rather than the Pro home shell.
/// </summary>
public partial class PublishPage : ComponentBase
{
    [Inject] private ProWorkspace Workspace { get; set; } = default!;
    [Inject] private SignInGate Gate { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;
    [Inject] private ILogger<PublishPage> Logger { get; set; } = default!;

    private bool _loaded;
    private bool _pledged;
    private bool _busy;
    private bool _phoneTaken;
    private bool _failed;

    protected override async Task OnInitializedAsync()
    {
        if (!await Workspace.EnsureLoadedAsync())
        {
            Nav.NavigateTo("/", replace: true);
            return;
        }

        if (Workspace.IsPublished)
        {
            Nav.NavigateTo(ProRoutes.Home, replace: true);
            return;
        }

        // The listing's number is the account's verified number (a Google sign-in adds one here first).
        if (await Gate.RedirectAsync(ProRoutes.Publish, requirePhone: true) is { } step)
        {
            Nav.NavigateTo(step, replace: true);
            return;
        }

        _loaded = true;
    }

    private async Task PublishAsync()
    {
        _busy = true;
        _failed = false;
        try
        {
            _phoneTaken = await Workspace.PublishAsync() == PublishResult.PhoneTaken;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Publishing the listing failed.");
            _failed = true;
        }
        finally
        {
            _busy = false;
        }

        if (!_phoneTaken && !_failed)
        {
            Nav.NavigateTo(ProRoutes.Home, replace: true);
        }
    }

    private void GoBack() => Nav.NavigateTo(ProRoutes.Home);
}
