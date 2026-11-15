using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Components.Shared;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Analytics;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Onboarding;

/// <summary>
/// The fork: are you looking for someone to do a job, or looking for work?
/// Tile ids are the <see cref="UserRole"/> names.
/// </summary>
public partial class RolePage : ComponentBase
{
    [Inject] private IOnboardingStore Store { get; set; } = default!;
    [Inject] private LocalAccount Account { get; set; } = default!;
    [Inject] private AppAnalytics Analytics { get; set; } = default!;
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
        if (state.Role != role)
        {
            // Switching sides starts that side's interview; answers for the other side are kept.
            state.Role = role;
            state.CompletedAt = null;
        }

        var path = role == UserRole.Seeker ? "/start/seeker/1" : "/start/pro/1";
        if (state.CompletedAt is null)
        {
            state.ResumePath = path;
        }

        await Store.SaveAsync(state);
        // The server's copy is a backup (it logs its own failure and the interview's last step saves it again), so
        // the tap never waits on it: a cold server or weak 4G used to leave this screen frozen for up to a minute.
        _ = Account.SaveProfileAsync();
        Analytics.SetAudience(role, state.Language);
        Nav.NavigateTo(path);
    }

    private void GoBack() => Nav.NavigateTo("/start");
}
