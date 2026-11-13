namespace SideGigPh.Maui.Models;

/// <summary>What a Seeker is looking for, as ids from <c>ReferenceData</c>. Free text is resolved to these first.</summary>
public sealed record SearchRequest(string? CategoryId, string? ServiceId, string? CityId, string? BudgetId);

/// <summary>
/// How free text like "tubero barado" was understood: the best category, a specific job when
/// one clearly fits, and other categories that also matched (offered as "did you mean").
/// </summary>
public sealed record SearchIntent(string? CategoryId, string? ServiceId, IReadOnlyList<string> OtherCategoryIds)
{
    public static SearchIntent None { get; } = new(null, null, []);

    public bool Matched => CategoryId is not null;
}

/// <summary>One pro in the results, with the reasons it ranked where it did (ranked by the server).</summary>
public sealed record SearchHit(
    ProfessionalListing Pro,
    ReviewSummary? Reviews,
    bool OffersService,
    bool ServesTown,
    bool FitsBudget);

/// <summary>
/// Ranked results loaded so far: pros who go to the Seeker's town first, then pros from other towns. They come a page
/// at a time; <see cref="InTownTotal"/> and <see cref="OtherTownsTotal"/> count every result, loaded or not.
/// </summary>
public sealed record SearchResult(
    IReadOnlyList<SearchHit> InTown,
    IReadOnlyList<SearchHit> OtherTowns,
    int InTownTotal,
    int OtherTownsTotal)
{
    public static SearchResult Empty { get; } = new([], [], 0, 0);

    /// <summary>How many results are loaded.</summary>
    public int Count => InTown.Count + OtherTowns.Count;

    /// <summary>How many results there are in all.</summary>
    public int Total => InTownTotal + OtherTownsTotal;

    public bool HasMore => Count < Total;

    /// <summary>
    /// These results with the next page added under them. Totals come from the newer page; a pro already shown (the
    /// ranking moved between pages) isn't shown twice.
    /// </summary>
    public SearchResult Append(SearchResult next)
    {
        var shown = InTown.Concat(OtherTowns).Select(h => h.Pro.Id).ToHashSet();
        return new SearchResult(
            [.. InTown, .. next.InTown.Where(h => shown.Add(h.Pro.Id))],
            [.. OtherTowns, .. next.OtherTowns.Where(h => shown.Add(h.Pro.Id))],
            next.InTownTotal,
            next.OtherTownsTotal);
    }
}

/// <summary>One row in the Seeker's Inbox: their chat with a pro, and the pro's listing (photo, trade).</summary>
public sealed record InboxThread(ProfessionalListing Pro, Chat Chat);
