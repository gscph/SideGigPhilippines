using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;

namespace SideGigPh.Maui.Components.Onboarding;

/// <summary>
/// <c>/switch/seeker</c> or <c>/switch/professional</c>, opened by the Me page's <see cref="Shared.SwitchSideRow"/>:
/// makes that side the one the app opens on (<see cref="LocalAccount.SwitchSideAsync"/>) and goes to its home, or to its
/// first question when it was never set up. Replaces itself in the history, so the phone's Back never lands here.
/// </summary>
public partial class SwitchSidePage : ComponentBase
{
    [Inject] private LocalAccount Account { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    [Parameter] public string? Side { get; set; }

    protected override async Task OnInitializedAsync()
    {
        if (SideSwitch.Parse(Side) is not { } role)
        {
            Nav.NavigateTo("/", replace: true);
            return;
        }

        var path = await Account.SwitchSideAsync(role);
        Nav.NavigateTo(path, replace: true);
    }
}
