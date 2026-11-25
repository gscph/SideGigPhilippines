using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Onboarding;

/// <summary>
/// The fork: are you looking for someone to do a job, or looking for work?
/// Tile ids are the <see cref="UserRole"/> names. Each opens its side's interview; a worker who signed in with Google or
/// Apple confirms their mobile number inside it (<see cref="ProStep.Number"/>), a customer is never asked.
/// </summary>
public partial class RolePage : ComponentBase
{
    [Inject] private IOnboardingStore Store { get; set; } = default!;
    [Inject] private LocalAccount Account { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    private OnboardingState? _state;
    private bool _busy;

    private IReadOnlyList<ChoiceItem> Options =>
    [
        new(nameof(UserRole.Professional), T.Get(Copy.RoleProfessional), AppImages.Worker, T.Get(Copy.RoleProfessionalHint)),
        new(nameof(UserRole.Seeker), T.Get(Copy.RoleSeeker), AppImages.Home, T.Get(Copy.RoleSeekerHint)),
    ];

    private IReadOnlyCollection<string> Selected =>
        _state?.Role is { } role ? new[] { role.ToString() } : Array.Empty<string>();

    protected override async Task OnInitializedAsync() => _state = await Store.LoadAsync();

    private async Task PickAsync(string id)
    {
        if (_busy || _state is null)
        {
            return;
        }

        _busy = true;
        var state = _state;
        var role = Enum.Parse<UserRole>(id);
        // A side never set up starts its interview; one already set up (the same account on both sides) opens its home.
        // The other side's answers are kept either way.
        var path = SideSwitch.MoveTo(state, role);
        await Store.SaveAsync(state);
        // The server's copy is a backup (it logs its own failure and the interview's last step saves it again), so
        // the tap never waits on it: a cold server or weak 4G used to leave this screen frozen for up to a minute.
        _ = Account.SaveProfileAsync();
        Nav.NavigateTo(path);
    }

    private void GoBack() => Nav.NavigateTo("/start");
}
