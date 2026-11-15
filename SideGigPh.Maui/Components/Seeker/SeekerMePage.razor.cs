using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Seeker;

/// <summary>
/// The Seeker's Me tab (also the avatar in the top bar): their one profile photo (add or change; a new one
/// replaces the old), their name (shown on their reviews), a link to their reviews (and pros still to rate),
/// their preferences (what they need, town, when, budget: each row opens that one question, which comes back to the
/// results), then the shared <see cref="Shared.MePage"/> parts (language, Start over).
/// </summary>
public partial class SeekerMePage : ComponentBase
{
    [Inject] private SeekerWorkspace Workspace { get; set; } = default!;
    [Inject] private LocalAccount Account { get; set; } = default!;
    [Inject] private BusyIndicator Busy { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;

    private string? _name;
    private bool _saved;
    private PhotoAddResult? _photoResult;
    private bool _busy;

    private bool NameChanged => (_name?.Trim() ?? string.Empty) != (Workspace.Answers.ReviewerName ?? string.Empty);

    /// <summary>"2 to rate" while some contacted pros are unrated, else how many reviews they wrote.</summary>
    private string ReviewsLabel => Workspace.ToRate.Count > 0
        ? T.Format(Copy.ToRateCount, Workspace.ToRate.Count)
        : Workspace.MyReviews.Count == 1 ? T.Get(Copy.ReviewsCountOne) : T.Format(Copy.ReviewsCount, Workspace.MyReviews.Count);

    private SeekerAnswers Answers => Workspace.Answers;

    /// <summary>"Aircon Technician · Linis ng aircon", or the trade alone while no job is picked.</summary>
    private string NeedLabel =>
        string.Join(" · ", new[]
        {
            ReferenceData.Find(ReferenceData.Categories, Answers.CategoryId),
            ReferenceData.Find(ReferenceData.ServicesOf(Answers.CategoryId), Answers.ServiceId),
        }.OfType<CatalogOption>().Select(option => T.Get(option.Label))) is { Length: > 0 } label ? label : NotSet;

    private string TownLabel => LabelOf(ReferenceData.Cities, Answers.CityId);

    private string WhenLabel => LabelOf(ReferenceData.Urgencies, Answers.UrgencyId);

    private string BudgetLabel => LabelOf(Pricing.BudgetChoices(Answers.ServiceId, Answers.CategoryId, Answers.BudgetId), Answers.BudgetId);

    private string NotSet => T.Get(Copy.PreferenceNotSet);

    protected override void OnInitialized() => _name = Workspace.Answers.ReviewerName;

    private string LabelOf(IReadOnlyList<CatalogOption> options, string? id) =>
        ReferenceData.Find(options, id) is { } option ? T.Get(option.Label) : NotSet;

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
