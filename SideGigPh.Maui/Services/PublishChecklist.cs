using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Services;

/// <summary>One promise on the worker's Publish checklist: a stable id, its words, and whether it links the Terms.</summary>
public sealed record Commitment(string Id, LocalizedText Text, bool LinksTerms = false);

/// <summary>
/// The promises a Skilled Worker ticks before going live (the first Publish step, <see cref="PublishStep.Checklist"/>):
/// honest work and prices, the Terms, the community rules, fair offers. Every one must be ticked, each by hand (none
/// starts ticked), before the Ready step. Kept here, not in the page, so the list and the rule are tested.
/// </summary>
public static class PublishChecklist
{
    public const string Honest = "honest";
    public const string Terms = "terms";
    public const string Community = "community";
    public const string FairOffers = "fair-offers";

    /// <summary>The promises, in the order shown.</summary>
    public static IReadOnlyList<Commitment> Items { get; } =
    [
        new(Honest, Copy.CommitHonest),
        new(Terms, Copy.CommitTerms, LinksTerms: true),
        new(Community, Copy.CommitCommunity),
        new(FairOffers, Copy.CommitFairOffers),
    ];

    /// <summary>Whether the worker may go on to the Ready step: every promise ticked.</summary>
    public static bool CanContinue(IReadOnlyCollection<string> ticked) => Items.All(item => ticked.Contains(item.Id));
}

/// <summary>
/// The promises ticked in this run of the app (<see cref="ProWorkspace.Commitments"/>), so Back from the Ready step
/// finds them still ticked. Only in memory, like the single pledge before it: a killed app asks again, and the server
/// never stores it.
/// </summary>
public sealed class PublishCommitments
{
    private readonly HashSet<string> _ticked = [];

    public bool IsTicked(string id) => _ticked.Contains(id);

    /// <summary>Ticks or unticks one promise; ids not on the checklist are ignored.</summary>
    public void Set(string id, bool ticked)
    {
        if (!PublishChecklist.Items.Any(item => item.Id == id))
        {
            return;
        }

        if (ticked)
        {
            _ticked.Add(id);
        }
        else
        {
            _ticked.Remove(id);
        }
    }

    public bool AllTicked => PublishChecklist.CanContinue(_ticked);

    public void Clear() => _ticked.Clear();
}
