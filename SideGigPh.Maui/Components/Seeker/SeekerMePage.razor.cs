using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Seeker;

/// <summary>
/// The Seeker's Me tab (also the avatar in the top bar): their one profile photo (add or change; a new one
/// replaces the old), their name (shown on their reviews), a link to their reviews (and pros still to rate), then the
/// "Become a Skilled Worker" (<c>BecomeWorkerCard</c>) or, once that side is set up, the switch row (<c>SwitchSideRow</c>), then the
/// shared <see cref="Shared.MePage"/> parts (language, Delete account). No "preferences": what they need changes with every
/// search, so it is the search itself (Home's box, filters and recent searches), never a setting.
/// </summary>
public partial class SeekerMePage : ComponentBase
{
    [Inject] private SeekerWorkspace Workspace { get; set; } = default!;
    [Inject] private LocalAccount Account { get; set; } = default!;
    [Inject] private BusyIndicator Busy { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;
    [Inject] private IOnboardingStore Store { get; set; } = default!;

    private string? _name;
    private bool _saved;
    private PhotoAddResult? _photoResult;
    private bool _busy;

    // The worker side is set up: a plain switch row; until then the "Become a Skilled Worker" card offers it.
    private bool _workerSetUp;

    private bool NameChanged => (_name?.Trim() ?? string.Empty) != (Workspace.Answers.ReviewerName ?? string.Empty);

    /// <summary>"2 to rate" while some contacted pros are unrated, else how many reviews they wrote.</summary>
    private string ReviewsLabel => Workspace.ToRate.Count > 0
        ? T.Format(Copy.ToRateCount, Workspace.ToRate.Count)
        : Workspace.MyReviews.Count == 1 ? T.Get(Copy.ReviewsCountOne) : T.Format(Copy.ReviewsCount, Workspace.MyReviews.Count);

    protected override async Task OnInitializedAsync()
    {
        _name = Workspace.Answers.ReviewerName;
        _workerSetUp = (await Store.LoadAsync()).HasFinished(UserRole.Professional);
    }

    private async Task SaveNameAsync()
    {
        await Workspace.SetNameAsync(_name);
        _saved = true;
    }

    private async Task ChangePhotoAsync()
    {
        if (_busy)
        {
            return; // a second tap before the button re-rendered as disabled (opened the photo picker twice)
        }

        _busy = true;
        try
        {
            _photoResult = await Busy.RunAsync(async () =>
            {
                var result = await Workspace.ChangeProfilePhotoAsync();
                if (result.Added > 0)
                {
                    await Account.SaveProfileAsync(); // the account's one profile photo, kept with the profile
                }

                return result;
            });
        }
        finally
        {
            _busy = false;
        }
    }
}
