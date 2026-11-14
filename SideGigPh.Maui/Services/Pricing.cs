using SideGigPh.Contracts;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Services;

/// <summary>
/// Prices from each job's guide (<see cref="ReferenceData.Prices"/>): what a Skilled Worker charges at their rate
/// (<see cref="PriceLevels"/>: the job's lowest, average or above-average price), the budget choices a Seeker gets for
/// their job, and how a budget (<see cref="BudgetRange"/>: <c>"800-1500"</c>) reads. Listings and budgets saved by app
/// versions before price guides carry an old price tier id; those still read (as the tier's typical price / its range).
/// </summary>
public static class Pricing
{
    /// <summary>The Seeker budget tile that opens the two amount boxes.</summary>
    public const string CustomBudget = "custom";

    /// <summary>A job's price guide; for a whole trade (no job) a guide over its jobs; null when nothing is known.</summary>
    public static PriceGuide? GuideFor(string? serviceId, string? categoryId = null)
    {
        if (serviceId is not null)
        {
            return ReferenceData.Prices.GetValueOrDefault(serviceId);
        }

        var guides = ReferenceData.ServicesOf(categoryId)
            .Select(service => ReferenceData.Prices.GetValueOrDefault(service.Id))
            .OfType<PriceGuide>()
            .ToList();
        return guides.Count == 0
            ? null
            : new PriceGuide(guides.Min(g => g.Min), RoundedMean(guides.Select(g => g.Average)), RoundedMean(guides.Select(g => g.High)));
    }

    /// <summary>A rate's price for one job: the guide at the level; null for "depends on the job", an old tier or an unpriced job.</summary>
    public static int? PriceAt(string? rateId, string serviceId) =>
        ReferenceData.Prices.GetValueOrDefault(serviceId) is { } guide ? At(guide, rateId) : null;

    /// <summary>The cheapest and dearest of a rate's prices over <paramref name="serviceIds"/>; null when none is priced.</summary>
    public static (int Low, int High)? RateRange(string? rateId, IEnumerable<string> serviceIds)
    {
        var prices = serviceIds.Select(id => PriceAt(rateId, id)).OfType<int>().ToList();
        return prices.Count == 0 ? null : (prices.Min(), prices.Max());
    }

    /// <summary>The rate tiles for a worker's jobs: each level with its price ("₱755 – ₱2,500 per job"), then "depends on the job".</summary>
    public static IReadOnlyList<CatalogOption> RateChoices(IReadOnlyCollection<string> serviceIds) =>
    [
        .. ReferenceData.RateLevels.Select(level => level with
        {
            Hint = RateRange(level.Id, serviceIds) is { } range ? PerJob(range) : null,
        }),
        ReferenceData.QuoteOption,
    ];

    /// <summary>
    /// The budget choices for a job (or a whole trade): lowest–average ("Tipid"), average–above average, above average
    /// and up, then <paramref name="current"/> when it is none of those (a typed range, an old tier) so it still shows
    /// as picked, then "not sure yet". Without a price guide, only those last two.
    /// </summary>
    public static IReadOnlyList<CatalogOption> BudgetChoices(string? serviceId, string? categoryId, string? current)
    {
        var choices = new List<CatalogOption>();
        if (GuideFor(serviceId, categoryId) is { } guide)
        {
            choices.Add(Budget(guide.Min, guide.Average, Copy.BudgetLow));
            choices.Add(Budget(guide.Average, guide.High, Copy.BudgetMid));
            choices.Add(Budget(guide.High, null, Copy.BudgetHigh));
        }

        if (current != ReferenceData.Unsure && BudgetLabel(current) is { } label && choices.All(choice => choice.Id != current))
        {
            choices.Add(new CatalogOption(current!, label, "✏️"));
        }

        choices.Add(ReferenceData.UnsureOption);
        return choices;
    }

    /// <summary>"₱800 – ₱1,500", "₱1,500 and up", "Up to ₱500", "Not sure yet"; null for no budget or one that doesn't read.</summary>
    public static LocalizedText? BudgetLabel(string? budgetId)
    {
        if (budgetId == ReferenceData.Unsure)
        {
            return ReferenceData.UnsureOption.Label;
        }

        if (!BudgetRange.TryParse(budgetId, out var min, out var max))
        {
            return null;
        }

        return max is not { } top ? Fill(Copy.BudgetAndUp, Money.Peso(min))
            : min == 0 ? Fill(Copy.BudgetUpTo, Money.Peso(top))
            : Range(min, top);
    }

    /// <summary>A typed budget the app accepts: from at least the minimum wage, up to the same or more.</summary>
    public static bool IsValidBudget(int? min, int? max) =>
        min is { } from && max is { } to && from >= ReferenceData.MinimumWage && to >= from;

    /// <summary>The two boxes' first values: the saved typed range, else the job's lowest to average price, else the minimum wage.</summary>
    public static (int Min, int Max) CustomStart(string? budgetId, string? serviceId, string? categoryId) =>
        BudgetRange.TryParse(budgetId, out var min, out var max) && max is { } top && min >= ReferenceData.MinimumWage ? (min, top)
        : GuideFor(serviceId, categoryId) is { } guide ? (guide.Min, guide.Average)
        : (ReferenceData.MinimumWage, ReferenceData.MinimumWage);

    /// <summary>"₱1,000" for one price, "₱755 – ₱2,500" for a spread.</summary>
    public static LocalizedText PriceText((int Low, int High) range) =>
        range.Low == range.High ? Same(Money.Peso(range.Low)) : Range(range.Low, range.High);

    private static LocalizedText PerJob((int Low, int High) range)
    {
        var price = PriceText(range);
        return new(string.Format(Copy.RatePerJob.English, price.English), string.Format(Copy.RatePerJob.Taglish, price.Taglish));
    }

    private static LocalizedText Same(string text) => new(text, text);

    private static CatalogOption Budget(int min, int? max, LocalizedText hint) =>
        new(BudgetRange.Format(min, max), max is { } top ? Range(min, top) : Fill(Copy.BudgetAndUp, Money.Peso(min)), Hint: hint);

    private static LocalizedText Range(int min, int max) => Fill(Copy.BudgetRange, Money.Peso(min), Money.Peso(max));

    // A price reads the same in both languages; only the words around it change.
    private static LocalizedText Fill(LocalizedText template, params object[] values) =>
        new(string.Format(template.English, values), string.Format(template.Taglish, values));

    private static int? At(PriceGuide guide, string? level) => level switch
    {
        PriceLevels.Minimum => guide.Min,
        PriceLevels.Average => guide.Average,
        PriceLevels.AboveAverage => guide.High,
        _ => null,
    };

    // To the nearest ₱50, so a mean reads like a price people quote.
    private static int RoundedMean(IEnumerable<int> amounts) => (int)Math.Round(amounts.Average() / 50, MidpointRounding.AwayFromZero) * 50;
}
