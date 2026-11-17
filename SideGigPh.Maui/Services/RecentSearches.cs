using SideGigPh.Contracts;
using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services;

/// <summary>
/// The household's recent searches and posts (<see cref="SeekerAnswers.RecentSearches"/>), kept only on the phone.
/// A search or post goes to the top; the same kind of work in the same town replaces the older one (its budget is
/// updated), and only the newest <see cref="Max"/> stay. Seeker home shows the ones still offered, each with Search and
/// Post: <see cref="Use"/> makes one the current search, which the results and the post form both start from.
/// </summary>
public static class RecentSearches
{
    /// <summary>How many are kept and shown.</summary>
    public const int Max = 3;

    /// <summary>The current search (trade, job, town, budget) goes to the top. Nothing before a trade is picked.</summary>
    public static void RememberCurrent(this SeekerAnswers answers)
    {
        if (answers.CategoryId is { } categoryId)
        {
            answers.Remember(new RecentSearch(categoryId, answers.ServiceId, OtherTextOf(answers.ServiceId, answers.OtherJobText),
                answers.CityId, answers.BudgetId));
        }
    }

    /// <summary>A posted request goes to the top, with its budget as a range ("up to" alone reads "under-…").</summary>
    public static void RememberPost(this SeekerAnswers answers, JobPostDraft post)
    {
        if (post.CategoryId is not { } categoryId)
        {
            return;
        }

        var budget = post.BudgetPesos is not { } upTo ? null
            : post.BudgetMinPesos is { } from and > 0 ? BudgetRange.Format(from, upTo)
            : $"under-{upTo}";
        var other = categoryId == ReferenceData.OtherTradeId ? post.Description : null;
        answers.Remember(new RecentSearch(categoryId, post.ServiceId, OtherTextOf(post.ServiceId, other), post.CityId, budget));

        // A barangay picked on a post in their home town fills their next post.
        if (post.CityId == answers.CityId && post.BarangayId is not null)
        {
            answers.PickBarangay(post.CityId, post.BarangayId);
        }
    }

    /// <summary>Makes <paramref name="recent"/> the current search (typed words no longer apply) and moves it to the top.</summary>
    public static void Use(this SeekerAnswers answers, RecentSearch recent)
    {
        answers.CategoryId = recent.CategoryId;
        answers.ServiceId = recent.ServiceId;
        answers.OtherJobText = recent.OtherJobText;
        answers.SearchText = null;
        if (recent.CityId is not null)
        {
            answers.CityId = recent.CityId;
            answers.AreaId = ReferenceData.AreaOf(recent.CityId);
        }

        if (recent.BudgetId is not null)
        {
            answers.BudgetId = recent.BudgetId;
        }

        answers.Remember(recent);
    }

    /// <summary>The ones to show: a trade (and job) still offered and a town still served; the rest wait unseen.</summary>
    public static IReadOnlyList<RecentSearch> Shown(this SeekerAnswers answers) =>
        answers.RecentSearches.Where(IsOffered).ToList();

    private static bool IsOffered(RecentSearch recent) =>
        ReferenceData.FindTrade(recent.CategoryId) is not null
        && (recent.ServiceId is null || ReferenceData.Find(ReferenceData.ServicesOf(recent.CategoryId), recent.ServiceId) is not null)
        && (recent.CityId is null || ReferenceData.AreaOf(recent.CityId) is not null);

    private static void Remember(this SeekerAnswers answers, RecentSearch recent)
    {
        answers.RecentSearches.RemoveAll(old => SameSearch(old, recent));
        answers.RecentSearches.Insert(0, recent);
        if (answers.RecentSearches.Count > Max)
        {
            answers.RecentSearches.RemoveRange(Max, answers.RecentSearches.Count - Max);
        }
    }

    // Same work in the same town; a new budget just updates it.
    private static bool SameSearch(RecentSearch a, RecentSearch b) =>
        a.CategoryId == b.CategoryId && a.ServiceId == b.ServiceId && a.CityId == b.CityId
        && string.Equals(a.OtherJobText ?? string.Empty, b.OtherJobText ?? string.Empty, StringComparison.OrdinalIgnoreCase);

    private static string? OtherTextOf(string? serviceId, string? typed) =>
        ReferenceData.IsOtherJob(serviceId) && typed?.Trim() is { Length: > 0 } text ? text : null;
}
