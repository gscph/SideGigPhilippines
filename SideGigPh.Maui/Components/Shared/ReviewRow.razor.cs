using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Shared;

/// <summary>One customer review: who, stars, when, what they said and which job it was.</summary>
public partial class ReviewRow : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    [Parameter, EditorRequired] public Review Review { get; set; } = default!;

    /// <summary>Who to show instead of the reviewer, e.g. the pro on the Seeker's own "Your reviews" list.</summary>
    [Parameter] public string? Name { get; set; }

    [Parameter] public string? PhotoUrl { get; set; }

    private string DisplayName => Name ?? Review.ReviewerName;

    private string? ServiceLabel => ReferenceData.FindService(Review.ServiceId) is { } service
        ? T.Get(service.Label)
        : null;
}
