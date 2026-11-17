using Microsoft.AspNetCore.Components;
using SideGigPh.Contracts;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Shared;

/// <summary>One customer review: who, stars, when, what they said and which job it was; the writer's own blind review says it is hidden for now.</summary>
public partial class ReviewRow : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    [Parameter, EditorRequired] public Review Review { get; set; } = default!;

    /// <summary>Who to show instead of the reviewer, e.g. the pro on the Seeker's own "Your reviews" list.</summary>
    [Parameter] public string? Name { get; set; }

    [Parameter] public string? PhotoUrl { get; set; }

    /// <summary>Where the small flag goes to report the review (another person's review only); no flag when null.</summary>
    [Parameter] public string? ReportHref { get; set; }

    private string DisplayName => Name ?? Review.ReviewerName;

    private static int BlindDays => (int)ReviewRules.BlindFor.TotalDays;

    private string? ServiceLabel => ReferenceData.FindService(Review.ServiceId) is { } service
        ? T.Get(service.Label)
        : null;
}
