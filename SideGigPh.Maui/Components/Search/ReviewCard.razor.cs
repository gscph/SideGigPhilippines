using Microsoft.AspNetCore.Components;
using SideGigPh.Contracts;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Search;

/// <summary>
/// One review in the profile's sideways row, like Airbnb's: who and which service, gold stars · when (· "Verified
/// service" after a completed job), then up to four lines of what they said. The full list (with the 🚩) is
/// <see cref="ProReviewsPage"/>.
/// </summary>
public partial class ReviewCard : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    [Parameter, EditorRequired] public Review Review { get; set; } = default!;

    private static int BlindDays => (int)ReviewRules.BlindFor.TotalDays;

    private string? ServiceLabel => ReferenceData.FindService(Review.ServiceId) is { } service ? T.Get(service.Label) : null;

    private string When => Review.VerifiedJob ? $"{T.Ago(Review.At)} · {T.Get(Copy.VerifiedService)}" : T.Ago(Review.At);
}
