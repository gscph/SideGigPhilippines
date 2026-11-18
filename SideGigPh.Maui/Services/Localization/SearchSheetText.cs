namespace SideGigPh.Maui.Services.Localization;

/// <summary>The answers the search sheet's collapsed rows and the results' summary pill show (tested).</summary>
public static class SearchSheetText
{
    /// <summary>A trade's job tiles in the What card: "All Aircon Technician services" first, then each job.</summary>
    public static IReadOnlyList<ChoiceItem> JobChoices(this AppText text, string categoryId)
    {
        var trade = ReferenceData.FindTrade(categoryId);
        var all = new ChoiceItem(SearchSheet.AllJobs, text.Format(Copy.SearchAllJobs, trade is null ? string.Empty : text.Get(trade.Label)),
            AppImages.For(categoryId));
        return [all, .. ChoiceItem.From(ReferenceData.ServicesOf(categoryId), text)];
    }

    /// <summary>What they typed, else the job, else the trade, else null (nothing picked yet).</summary>
    public static string? What(this AppText text, string? words, string? categoryId, string? serviceId)
    {
        if (!string.IsNullOrWhiteSpace(words))
        {
            return words.Trim();
        }

        if (categoryId is null)
        {
            return null;
        }

        var option = ReferenceData.Find(ReferenceData.ServicesOf(categoryId), serviceId) ?? ReferenceData.FindTrade(categoryId);
        return option is null ? null : text.Get(option.Label);
    }

    /// <summary>The town's name, or null for a town we don't serve.</summary>
    public static string? Where(this AppText text, string? townId) =>
        ReferenceData.Find(ReferenceData.Cities, townId) is { } town ? text.Get(town.Label) : null;

    /// <summary>"Today, it's urgent"… or "Any time".</summary>
    public static string When(this AppText text, string? urgencyId) =>
        ReferenceData.Find(ReferenceData.Urgencies, urgencyId) is { } urgency ? text.Get(urgency.Label) : text.Get(Copy.SheetAnyTime);

    /// <summary>The results' pill, line 1: "Aircon cleaning in Angono" (or just one of them).</summary>
    public static string SummaryTitle(this AppText text, string? what, string? where) => (what, where) switch
    {
        ({ } w, { } t) => text.Format(Copy.SheetWhatWhere, w, t),
        ({ } w, null) => w,
        (null, { } t) => t,
        _ => text.Get(Copy.SearchStart),
    };

    /// <summary>The results' pill, line 2: "Any time · Any budget", or the picked ones.</summary>
    public static string SummaryNote(this AppText text, string? urgencyId, string? budgetId)
    {
        var budget = budgetId != ReferenceData.Unsure && Pricing.BudgetLabel(budgetId) is { } label ? text.Get(label) : text.Get(Copy.SheetAnyBudget);
        return $"{text.When(urgencyId)} · {budget}";
    }
}
