using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services;

/// <summary>The search sheet's questions, in the order they are asked.</summary>
public enum SearchStep
{
    What,
    Where,
    When,
}

/// <summary>
/// The full-screen search sheet's draft, like Airbnb's "Where? / When? / What?": one question open at a time, the others
/// collapsed to a row with their answer. Nothing is saved until Search (the page then hands the answers to
/// <see cref="SeekerWorkspace"/>), so Close leaves the current search as it was.
/// </summary>
public sealed class SearchSheet
{
    /// <summary>Typed words ("aircon linis"); <see cref="SeekerWorkspace.SearchForAsync"/> works out the job on Search.</summary>
    public string? Words { get; private set; }

    public string? CategoryId { get; private set; }

    /// <summary>The picked job (a tile under the trade), or null for all of the trade's jobs.</summary>
    public string? ServiceId { get; private set; }

    /// <summary>The "All" tile's id among a trade's job tiles (all of the trade's jobs: <see cref="ServiceId"/> null).</summary>
    public const string AllJobs = "all";

    public string? TownId { get; private set; }

    public string? UrgencyId { get; private set; }

    public SearchStep Open { get; private set; } = SearchStep.What;

    /// <summary>A search needs words or a trade; the town is pre-picked (the launch town) and When is optional.</summary>
    public bool CanSearch => !string.IsNullOrWhiteSpace(Words) || CategoryId is not null;

    /// <summary>The sheet opened on the current search, its What question open.</summary>
    public static SearchSheet From(SeekerAnswers answers) => new()
    {
        Words = answers.SearchText,
        CategoryId = answers.CategoryId,
        ServiceId = answers.ServiceId,
        TownId = answers.TownShown(),
        UrgencyId = answers.UrgencyId,
    };

    public void OpenStep(SearchStep step) => Open = step;

    /// <summary>
    /// Words typed in the What box; a trade picked earlier no longer applies. Clearing the words of an "Others" search
    /// (typed words that matched no trade) clears that too, so What asks again instead of searching "Others" with nothing.
    /// </summary>
    public void Type(string? words)
    {
        Words = string.IsNullOrWhiteSpace(words) ? null : words.Trim();
        if (Words is not null || CategoryId == ReferenceData.OtherTradeId)
        {
            CategoryId = null;
            ServiceId = null;
        }
    }

    /// <summary>
    /// A trade tile: all its jobs ("All" picked) and the typed words cleared; What stays open so its job tiles show. The
    /// same tile always means the same thing, whatever was searched before.
    /// </summary>
    public void PickTrade(string categoryId)
    {
        CategoryId = categoryId;
        ServiceId = null;
        Words = null;
    }

    /// <summary>A job tile under the trade (<see cref="AllJobs"/> for all of them), then on to Where.</summary>
    public void PickJob(string jobId)
    {
        ServiceId = jobId == AllJobs ? null : jobId;
        Open = SearchStep.Where;
    }

    /// <summary>A town, then on to When.</summary>
    public void PickTown(string townId)
    {
        TownId = townId;
        Open = SearchStep.When;
    }

    public void PickUrgency(string urgencyId) => UrgencyId = urgencyId;

    /// <summary>
    /// On Search with no words: true when the trade or job differs from the current search (or replaces words searched
    /// before), so the page picks the trade, then the job when one was picked.
    /// </summary>
    public bool NeedsJobPick(SeekerAnswers current) =>
        Words is null && CategoryId is not null
        && (CategoryId != current.CategoryId || ServiceId != current.ServiceId || current.SearchText is not null);

    /// <summary>"Clear all": back to an empty search in the launch town, What open.</summary>
    public void Clear()
    {
        Words = null;
        CategoryId = null;
        ServiceId = null;
        UrgencyId = null;
        TownId = ReferenceData.DefaultTownId;
        Open = SearchStep.What;
    }
}
