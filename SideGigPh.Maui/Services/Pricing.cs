using SideGigPh.Contracts;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Services;

/// <summary>
/// Prices from each job's guide (<see cref="ReferenceData.Prices"/>), only ever a guide: the low / average / high prices
/// a Skilled Worker sees when typing their own average price (<see cref="GuideOver"/>), the budget choices a Seeker gets
/// for their job, and how a budget (<see cref="BudgetRange"/>: <c>"800-1500"</c>) reads.
/// </summary>
public static class Pricing
{
    /// <summary>A job's price guide; for a whole trade (no job) a guide over its jobs; null when nothing is known.</summary>
    public static PriceGuide? GuideFor(string? serviceId, string? categoryId = null) =>
        serviceId is not null
            ? ReferenceData.Prices.GetValueOrDefault(serviceId)
            : GuideOver(ReferenceData.ServicesOf(categoryId).Select(service => service.Id));

    /// <summary>
    /// A guide over several jobs (a worker's jobs beside their price box, a whole trade): the lowest low, and the average
    /// and high prices on average; null when none is priced (an "Others" job).
    /// </summary>
    public static PriceGuide? GuideOver(IEnumerable<string> serviceIds)
    {
        var guides = serviceIds.Select(id => ReferenceData.Prices.GetValueOrDefault(id)).OfType<PriceGuide>().ToList();
        return guides.Count == 0
            ? null
            : new PriceGuide(guides.Min(g => g.Min), RoundedMean(guides.Select(g => g.Average)), RoundedMean(guides.Select(g => g.High)));
    }

    /// <summary>
    /// The budget choices for a job (or a whole trade): lowest–average ("Tipid"), average–above average, above average
    /// and up, then <paramref name="current"/> when it is none of those (a typed range) so it still shows
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
            choices.Add(new CatalogOption(current!, label, AppImages.Custom));
        }

        choices.Add(ReferenceData.UnsureOption);
        return choices;
    }

    /// <summary>"₱800 – ₱1,500", "₱1,500 and up", "Up to ₱1,500", "Not sure yet"; null for no budget or one that doesn't read.</summary>
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

    /// <summary>
    /// A typed budget the app accepts: from more than ₱0, up to the same or more. The job's guide is only shown beside
    /// the boxes: the household pays after the work is done, at the price the two agree on.
    /// </summary>
    public static bool IsValidBudget(int? min, int? max) =>
        min is { } from && max is { } to && from > 0 && to >= from && to <= JobPostRules.MaxBudgetPesos;

    /// <summary>The two boxes' first values: the saved range, else the job's low to average price, else empty.</summary>
    public static (int? Min, int? Max) CustomStart(string? budgetId, string? serviceId, string? categoryId) =>
        BudgetRange.TryParse(budgetId, out var min, out var max) && max is { } top ? (min, top)
        : GuideFor(serviceId, categoryId) is { } guide ? (guide.Min, guide.Average)
        : (null, null);

    /// <summary>"Usual price: ₱755 – ₱1,500. Only a guide." for a job (or a whole trade); null without a guide.</summary>
    public static LocalizedText? GuideNote(string? serviceId, string? categoryId) =>
        GuideFor(serviceId, categoryId) is { } guide ? Fill(Copy.PriceGuideNote, Money.Peso(guide.Min), Money.Peso(guide.High)) : null;

    /// <summary>A job post's budget: "₱800 – ₱1,500" with a low end, "₱1,500" with only a top.</summary>
    public static string PostBudget(JobPost post) => PostBudget(post.BudgetMinPesos, post.BudgetPesos);

    /// <summary>A budget from–up to: "₱800 – ₱1,500" with a low end, "₱1,500" with only a top.</summary>
    public static string PostBudget(int? fromPesos, int upToPesos) =>
        fromPesos is { } from && from != upToPesos
            ? string.Format(Copy.BudgetRange.English, Money.Peso(from), Money.Peso(upToPesos))
            : Money.Peso(upToPesos);

    private static CatalogOption Budget(int min, int? max, LocalizedText hint) =>
        new(BudgetRange.Format(min, max), max is { } top ? Range(min, top) : Fill(Copy.BudgetAndUp, Money.Peso(min)), Hint: hint);

    private static LocalizedText Range(int min, int max) => Fill(Copy.BudgetRange, Money.Peso(min), Money.Peso(max));

    // A price reads the same in both languages; only the words around it change.
    private static LocalizedText Fill(LocalizedText template, params object[] values) =>
        new(string.Format(template.English, values), string.Format(template.Taglish, values));

    // To the nearest ₱50, so a mean reads like a price people quote.
    private static int RoundedMean(IEnumerable<int> amounts) => (int)Math.Round(amounts.Average() / 50, MidpointRounding.AwayFromZero) * 50;
}
