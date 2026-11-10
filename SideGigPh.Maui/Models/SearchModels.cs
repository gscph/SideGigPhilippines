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

/// <summary>One pro in the results, with the reasons it ranked where it did.</summary>
public sealed record SearchHit(
    ProfessionalListing Pro,
    ReviewSummary? Reviews,
    bool OffersService,
    bool ServesTown,
    bool FitsBudget,
    double Score);

/// <summary>
/// Ranked results: pros who go to the Seeker's town first, then pros from other towns.
/// <see cref="ServiceFallback"/> means nobody lists the exact job yet, so these are the
/// category's pros instead.
/// </summary>
public sealed record SearchResult(IReadOnlyList<SearchHit> InTown, IReadOnlyList<SearchHit> OtherTowns, bool ServiceFallback)
{
    public static SearchResult Empty { get; } = new([], [], false);

    public int Count => InTown.Count + OtherTowns.Count;
}

/// <summary>Everything a Seeker sent one pro, newest last: one row in their Inbox.</summary>
public sealed record InboxThread(ProfessionalListing Pro, IReadOnlyList<Inquiry> Contacts)
{
    public Inquiry Latest => Contacts[^1];
}
