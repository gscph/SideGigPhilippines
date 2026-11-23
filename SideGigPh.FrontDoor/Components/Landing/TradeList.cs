using SideGigPh.FrontDoor.Localization;

namespace SideGigPh.FrontDoor.Components.Landing;

/// <summary>A trade (or a Construction job) the page shows, with the app's own picture.</summary>
public sealed record Trade(string Icon, Say Label);

/// <summary>
/// The trades in the app's order. Copied by hand: the page doesn't read the API's <c>GET config</c>, so keep it in step with
/// SideGigPh.AzureSQL/Sql/004-config.sql when a trade is added or retired (pictures: SideGigPh.Maui/Services/AppImages.cs).
/// </summary>
public static class TradeList
{
    public static readonly Trade[] Trades =
    [
        new("images/icons/aircon.svg", new("Aircon", "Aircon")),
        new("images/icons/construction.svg", new("Construction", "Construction")),
        new("images/icons/errand.svg", new("Errands", "Errands")),
        new("images/icons/it.svg", new("IT", "IT")),
        new("images/icons/mechanic.svg", new("Car Mechanic", "Mekaniko")),
        new("images/icons/pest-control.svg", new("Pest Control", "Pest Control")),
    ];

    public static readonly Trade[] ConstructionJobs =
    [
        new("images/icons/foreman.svg", new("Foreman", "Foreman")),
        new("images/icons/carpentry.svg", new("Carpentry", "Karpintero")),
        new("images/icons/electrical.svg", new("Electrical", "Electrical")),
        new("images/icons/plumbing.svg", new("Plumbing", "Tubero")),
        new("images/icons/masonry.svg", new("Masonry", "Mason")),
        new("images/icons/tiles.svg", new("Tiles", "Tiles")),
        new("images/icons/weld.svg", new("Welding", "Welding")),
        new("images/icons/roofing.svg", new("Roofing & gutter", "Bubong at alulod")),
        new("images/icons/rebar.svg", new("Steelman", "Steelman")),
        new("images/icons/worker.svg", new("Helper", "Helper")),
    ];

    /// <summary>The towns, launch town first (the app's <c>default-town</c>).</summary>
    public static readonly (string Name, bool First)[] Towns = [("Angono", true), ("Antipolo", false), ("Taytay", false)];
}
