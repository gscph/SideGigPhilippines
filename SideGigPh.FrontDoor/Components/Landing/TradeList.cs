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

    /// <summary>
    /// What each trade covers, in the words people search (marketing/seo/keywords.md): the English keeps the Tagalog
    /// word in brackets, since Google indexes the English page. From the jobs in 004-config.sql, most asked first.
    /// </summary>
    public static readonly (string Icon, Say Name, Say Covers)[] Services =
    [
        ("images/icons/aircon.svg", new("Aircon technician", "Aircon technician"),
            new("Aircon cleaning (linis aircon), aircon repair, leaking aircon, freon recharge (pa-freon), aircon installation (pakabit), transfer and PMS. Split type and window type.",
                "Linis aircon, sirang aircon, tumutulong aircon, pa-freon, pakabit at palipat aircon, aircon PMS. Split type at window type.")),
        ("images/icons/construction.svg", new("Construction", "Construction"),
            new("Plumber (tubero), electrician, carpenter (karpintero), mason, tiles, welder, roof and gutter repair (bubong, alulod), steelman, foreman and helper.",
                "Tubero, electrician, karpintero, mason, tiles, welder, ayos bubong at alulod, steelman, foreman at helper.")),
        ("images/icons/pest-control.svg", new("Pest control", "Pest control"),
            new("Termite treatment (anay), cockroaches (ipis), rats (daga), mosquito misting (lamok), bed bugs (surot), ants (langgam) and whole-house treatment.",
                "Pa-spray sa anay, ipis, daga, pa-misting sa lamok, surot, langgam, at spray sa buong bahay.")),
        ("images/icons/errand.svg", new("Errands", "Errands"),
            new("House cleaning (linis bahay), grocery runs, bills payment, PasaBuy, document delivery, parcel pickup, car cleaning and gardening.",
                "Linis bahay, pa-grocery, bills payment, PasaBuy, pahatid papeles, pa-pickup parcel, linis kotse at paghahalaman.")),
        ("images/icons/mechanic.svg", new("Car mechanic", "Mekaniko"),
            new("Car mechanic (mekaniko): suspension, engine (makina) and car electronics.",
                "Mekaniko ng kotse: suspension, makina at electronics.")),
        ("images/icons/it.svg", new("IT", "IT"),
            new("Computers and hardware, software, cybersecurity, AI and business systems.",
                "Computer at hardware, software, cybersecurity, AI at business systems.")),
    ];

    /// <summary>The towns, launch town first (the app's <c>default-town</c>).</summary>
    public static readonly (string Name, bool First)[] Towns = [("Angono", true), ("Antipolo", false), ("Taytay", false)];
}
