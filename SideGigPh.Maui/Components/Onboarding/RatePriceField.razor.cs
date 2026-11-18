using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Onboarding;

/// <summary>
/// A Skilled Worker's price: one peso box (their average price per service), a note that it is only a guide for
/// customers and not the final amount they pay, and what others charge for the worker's jobs (low / average / high
/// from the price guides, <see cref="Pricing.GuideOver"/>) as chips that fill the box in one tap. No guide for an
/// "Others" job: the box alone.
/// </summary>
public partial class RatePriceField : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    [Parameter] public int? Value { get; set; }

    [Parameter] public EventCallback<int?> ValueChanged { get; set; }

    /// <summary>The worker's jobs: the guide shown is over these.</summary>
    [Parameter, EditorRequired] public IReadOnlyCollection<string> ServiceIds { get; set; } = [];

    private PriceGuide? Guide => Pricing.GuideOver(ServiceIds);

    private static IEnumerable<(LocalizedText Label, int Pesos)> GuideChoices(PriceGuide guide) =>
    [
        (Copy.RateGuideLow, guide.Min),
        (Copy.RateGuideAverage, guide.Average),
        (Copy.RateGuideHigh, guide.High),
    ];
}
