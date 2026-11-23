using SideGigPh.FrontDoor.Localization;

namespace SideGigPh.FrontDoor.Components.Landing;

/// <summary>
/// Four number cards with real counts only, worked out from the lists the page already shows: trades, Construction's
/// jobs, towns, and the badges a worker can ask us to check. Never users, sign-ups or ratings before there are any.
/// </summary>
public partial class NumbersSection
{
    /// <summary>The badges in the app: government ID, TESDA, past project (CLAUDE.md, Worker verification).</summary>
    private const int Badges = 3;

    private sealed record Item(int Count, Say Title, Say Text);

    private static readonly Item[] Items =
    [
        new(TradeList.Trades.Length, Copy.Numbers.TradesTitle, Copy.Numbers.TradesText),
        new(TradeList.ConstructionJobs.Length, Copy.Numbers.ConstructionTitle, Copy.Numbers.ConstructionText),
        new(TradeList.Towns.Length, Copy.Numbers.TownsTitle, Copy.Numbers.TownsText),
        new(Badges, Copy.Numbers.BadgesTitle, Copy.Numbers.BadgesText),
    ];
}
