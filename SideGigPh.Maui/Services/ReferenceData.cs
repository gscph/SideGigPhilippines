using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services;

/// <summary>
/// The fixed answer lists the interview offers, in English and Taglish. Taglish uses the words
/// customers actually search with: English technical terms stay (split-type, chemical wash,
/// freon, breaker, PMS, CCTV, waterproofing), local words where they're the norm (CR, gripo,
/// lababo, yero, alulod, kisame, semento, poso), and Pa-/Paki- only where it sounds natural.
/// Job labels stand on their own ("Pa-linis ng aircon", not "Linis") so they read correctly
/// without their heading and drop straight into the pre-filled message to a pro.
/// </summary>
public static class ReferenceData
{
    /// <summary>Seeker budget answer: "not sure yet".</summary>
    public const string Unsure = "unsure";

    /// <summary>Professional rate answer: "depends on the job".</summary>
    public const string Quote = "quote";

    public const string UrgencyToday = "today";
    public const string UrgencyThisWeek = "this-week";
    public const string UrgencyCanvassing = "canvassing";

    // Category and service IDs are stored in saved answers and listings: never rename them,
    // only their labels. Within each category, services are listed most-requested first, and
    // every category ends with an "Other" option so nobody is forced into a wrong pick.
    public static readonly IReadOnlyList<CatalogOption> Categories =
    [
        new("aircon", new("Aircon", "Aircon Tech"), "❄️", new("Cleaning, repair, installation", "Linis, repair, pakabit")),
        new("plumbing", new("Plumbing", "Tubero"), "🚰", new("Clogs, leaks, faucets, CR", "Barado, tagas, gripo, CR")),
        new("electrical", new("Electrical", "Electrician"), "⚡", new("Power, breaker, outlets, lights", "Kuryente, breaker, saksakan, ilaw")),
        new("roofing", new("Roofing & Gutter", "Bubong at Alulod"), "🏠", new("Roof leaks, gutters, waterproofing", "Tulo sa bubong, yero, alulod, waterproofing")),
        new("carpentry", new("Carpentry", "Karpintero"), "🔨", new("Furniture, doors, cabinets", "Custom furniture, pinto, cabinet")),
        new("welder", new("Welding", "Welder"), "🔥", new("Gates, grills, railings", "Gate, grills, rehas, pa-weld")),
        new("masonry_tiling", new("Masonry & Tiling", "Mason & Tile Setter"), "🧱", new("Tiles, cement work, walls", "Tiles, semento, palitada, pader")),
        new("errand_runner", new("Errands & Delivery", "Errand Runner / PasaBuy"), "🛵", new("Grocery, bills payment, documents", "Pa-grocery, pabayad ng bills, hatid-papeles")),
        new("solar_cleaning", new("Solar Panel Cleaning", "Solar Panel Cleaning"), "☀️", new("Panel cleaning and check-up", "Linis at check-up ng solar panel")),
        new("home_massage", new("Home Service Massage", "Home Service Massage"), "💆", new("Full body, hilot, ventosa, reflexology", "Full body, hilot, ventosa, reflexology")),
    ];

    public static readonly IReadOnlyDictionary<string, IReadOnlyList<CatalogOption>> ServicesByCategory =
        new Dictionary<string, IReadOnlyList<CatalogOption>>
        {
            ["aircon"] =
            [
                new("aircon-cleaning", new("Aircon cleaning", "Pa-linis ng aircon"), "🧽"),               
                new("aircon-repair", new("Aircon not cooling / repair", "Hindi lumalamig / pa-repair ng aircon"), "🌡️"),
                new("aircon-leak", new("Aircon leaking water", "Tumutulo ang aircon"), "💧"),
                new("aircon-freon", new("Freon recharge", "Pa-freon ng aircon"), "🧪"),
                new("aircon-install", new("Aircon installation (window / split-type)", "Pakabit ng aircon (window / split-type)"), "🔧"),
                new("aircon-relocate", new("Aircon removal or transfer", "Pabaklas o palipat ng aircon"), "🚚"),
                new("aircon-pms", new("Aircon PMS / check-up", "PMS / check-up ng aircon"), "📋"),
                new("aircon-other", new("Other aircon job", "Iba pang aircon job"), "➕"),
            ],
            ["plumbing"] =
            [
                new("plumbing-clog", new("Clogged sink, drain or CR", "Barado ang lababo, drain o CR"), "🪠"),
                new("plumbing-leak", new("Leaking pipe or faucet", "May tagas ang tubo o gripo"), "💧"),
                new("plumbing-toilet", new("Toilet / CR repair", "Sirang inidoro / CR"), "🚽"),
                new("plumbing-replace", new("Replace faucet or pipe", "Palit ng gripo o tubo"), "🔧"),
                new("plumbing-install", new("Install sink, toilet or shower", "Pakabit ng lababo, inidoro o shower"), "🚿"),
                new("plumbing-water-heater", new("Water heater install or repair", "Pakabit o repair ng water heater"), "♨️"),
                new("plumbing-pump", new("Water pump, tank or poso", "Water pump, tangke o poso"), "🛢️"),
                new("plumbing-septic", new("Septic tank siphoning", "Pa-siphon ng poso negro"), "🚛"),
                new("plumbing-other", new("Other plumbing job", "Iba pang tubero job"), "➕"),
            ],
            ["electrical"] =
            [
                new("electrical-no-power", new("No power / breaker keeps tripping", "Walang kuryente / nagta-trip ang breaker"), "🔌"),
                new("electrical-outlet", new("Outlet or switch repair", "Sirang outlet o switch"), "🔧"),
                new("electrical-lights", new("Lights or ceiling fan", "Pakabit ng ilaw o ceiling fan"), "💡"),
                new("electrical-wiring", new("Wiring or rewiring", "Wiring o rewiring"), "🧵"),
                new("electrical-breaker-panel", new("Breaker or panel box", "Palit ng breaker o panel box"), "🗃️"),
                new("electrical-meter", new("Meralco meter / new line", "Pakabit ng metro / bagong linya (Meralco)"), "📟"),
                new("electrical-cctv", new("CCTV installation", "Pakabit ng CCTV"), "📹"),
                new("electrical-other", new("Other electrical job", "Iba pang electrical job"), "➕"),
            ],
            ["roofing"] =
            [
                new("roofing-leak", new("Roof leak repair", "Tulo sa bubong"), "🌧️"),
                new("roofing-gutter", new("Gutter cleaning or repair", "Linis o repair ng alulod"), "🍂"),
                new("roofing-sheet", new("Roof sheet replacement", "Palit ng yero"), "🔩"),
                new("roofing-waterproofing", new("Waterproofing (roof deck, walls)", "Waterproofing ng roof deck o pader"), "🛡️"),
                new("roofing-paint", new("Roof painting", "Pintura ng bubong"), "🎨"),
                new("roofing-other", new("Other roofing job", "Iba pang bubong job"), "➕"),
            ],
            ["carpentry"] =
            [
                new("carpentry-furniture", new("Furniture repair", "Repair ng furniture"), "🪑"),
                new("carpentry-door-window", new("Door or window repair", "Repair ng pinto o bintana"), "🚪"),
                new("carpentry-cabinet", new("Custom cabinets or shelves", "Pagawa ng cabinet o estante"), "🗄️"),
                new("carpentry-ceiling-roof", new("Ceiling repair", "Repair ng kisame"), "🏠"),
                new("carpentry-partition", new("Partition or drywall", "Partition o drywall"), "🧱"),
                new("carpentry-other", new("Other carpentry job", "Iba pang karpintero job"), "➕"),
            ],
            ["welder"] =
            [
                new("welder-repair", new("Welding repair", "Pa-weld ng sira"), "🔥"),
                new("welder-gate", new("Gate (new or repair)", "Pagawa o repair ng gate"), "🚧"),
                new("welder-grills", new("Window grills", "Grills sa bintana"), "🪟"),
                new("welder-railings", new("Stair or balcony railings", "Rehas sa hagdan o balcony"), "🪜"),
                new("welder-frame", new("Steel frame or trusses", "Bakal na frame o trusses"), "🏗️"),
                new("welder-other", new("Other welding job", "Iba pang welding job"), "➕"),
            ],
            ["masonry_tiling"] =
            [
                new("masonry-tiles", new("Tile installation or replacement", "Pakabit o palit ng tiles"), "🔲"),
                new("masonry-cement", new("Cement work / plastering", "Semento / palitada"), "🪣"),
                new("masonry-crack", new("Crack repair (wall or floor)", "Ayos ng bitak sa pader o sahig"), "🔨"),
                new("masonry-wall", new("Wall or fence (hollow blocks)", "Pader o bakod (hollow blocks)"), "🧱"),
                new("masonry-cr-renovation", new("CR / bathroom renovation", "Renovate ng CR / banyo"), "🚿"),
                new("masonry-other", new("Other masonry job", "Iba pang mason job"), "➕"),
            ],
            ["errand_runner"] =
            [
                new("errand-grocery", new("Grocery or market run", "Pa-grocery o pa-palengke"), "🛒"),
                new("errand-bills", new("Bills payment", "Pabayad ng bills"), "🧾"),
                new("errand-pasabuy", new("PasaBuy (buy and deliver)", "PasaBuy"), "🛍️"),
                new("errand-documents", new("Document pickup or delivery", "Pakuha o pahatid ng papeles"), "📄"),
                new("errand-parcel", new("Parcel pickup or drop-off", "Pa-pickup o pa-drop off ng parcel"), "📦"),
                new("errand-other", new("Other errand", "Iba pang utos"), "➕"),
            ],
            ["solar_cleaning"] =
            [
                new("solar-cleaning", new("Solar panel cleaning", "Pa-linis ng solar panel"), "🧽"),
                new("solar-checkup", new("Solar check-up / maintenance", "Check-up o maintenance ng solar"), "📋"),
                new("solar-inverter", new("Inverter or battery check", "Check ng inverter o battery"), "🔋"),
                new("solar-other", new("Other solar job", "Iba pang solar job"), "➕"),
            ],
            ["home_massage"] =
            [
                new("massage-full-body", new("Full body massage", "Full body massage"), "💆"),
                new("massage-hilot", new("Hilot", "Hilot"), "🤲"),
                new("massage-ventosa", new("Ventosa", "Ventosa"), "🫙"),
                new("massage-foot", new("Foot reflexology", "Foot reflexology"), "🦶"),
                new("massage-other", new("Other massage", "Iba pang massage"), "➕"),
            ],
        };

    /// <summary>The towns we serve for now (Rizal).</summary>
    public static readonly IReadOnlyList<CatalogOption> Cities =
    [
        new("angono", new("Angono", "Angono"), "📍"),
        new("antipolo", new("Antipolo", "Antipolo"), "📍"),
        new("binangonan", new("Binangonan", "Binangonan"), "📍"),
        new("taytay", new("Taytay", "Taytay"), "📍"),
    ];

    public static readonly IReadOnlyList<CatalogOption> Urgencies =
    [
        new(UrgencyToday, new("Today, it's urgent", "Ngayon na, urgent"), "⚡"),
        new(UrgencyThisWeek, new("This week", "Ngayong linggo"), "📅"),
        new(UrgencyCanvassing, new("Just comparing prices", "Nagca-canvass lang"), "🔍"),
    ];

    public static readonly IReadOnlyList<CatalogOption> Experience =
    [
        new("under-1", new("Less than 1 year", "Wala pang 1 taon"), "🌱"),
        new("1-3", new("1–3 years", "1–3 taon"), "🧰"),
        new("3-10", new("3–10 years", "3–10 taon"), "🛠️"),
        new("10-plus", new("10+ years", "Mahigit 10 taon"), "🏆"),
    ];

    /// <summary>
    /// The one price scale both sides answer on, cheapest first, so a Seeker's budget and a
    /// Professional's rate can be compared. <see cref="CatalogOption.Amount"/> is a typical
    /// job price inside the tier.
    /// </summary>
    public static readonly IReadOnlyList<CatalogOption> PriceTiers =
    [
        new("under-500", new("Below ₱500", "Mas mababa sa ₱500"), Amount: 400),
        new("500-1000", new("₱500 – ₱1,000", "₱500 – ₱1,000"), Amount: 750),
        new("1000-2000", new("₱1,000 – ₱2,000", "₱1,000 – ₱2,000"), Amount: 1500),
        new("2000-5000", new("₱2,000 – ₱5,000", "₱2,000 – ₱5,000"), Amount: 3500),
        new("5000-up", new("₱5,000 and up", "₱5,000 pataas"), Amount: 5000),
    ];

    public static readonly IReadOnlyList<CatalogOption> BudgetOptions =
    [
        .. PriceTiers,
        new(Unsure, new("Not sure yet", "Di ko pa alam"), "🤔"),
    ];

    public static readonly IReadOnlyList<CatalogOption> RateOptions =
    [
        .. PriceTiers,
        new(Quote, new("Depends on the job", "Depende sa trabaho"), "💬"),
    ];

    /// <summary>Monthly earnings goals. <see cref="CatalogOption.Amount"/> is pesos per month.</summary>
    public static readonly IReadOnlyList<CatalogOption> IncomeGoals =
    [
        new("5000", new("₱5,000", "₱5,000"), "🎯", Amount: 5000),
        new("10000", new("₱10,000", "₱10,000"), "🎯", Amount: 10000),
        new("20000", new("₱20,000", "₱20,000"), "🎯", Amount: 20000),
        new("30000-up", new("₱30,000 or more", "₱30,000 pataas"), "🎯", Amount: 30000),
    ];

    public static CatalogOption? Find(IEnumerable<CatalogOption> options, string? id) =>
        id is null ? null : options.FirstOrDefault(o => o.Id == id);

    public static CatalogOption? FindService(string? id) =>
        Find(ServicesByCategory.Values.SelectMany(services => services), id);

    public static string? CategoryOfService(string serviceId) =>
        ServicesByCategory.FirstOrDefault(pair => pair.Value.Any(s => s.Id == serviceId)).Key;

    /// <summary>Position on <see cref="PriceTiers"/> (0 = cheapest), or -1 for "not sure" / "quote" / unanswered.</summary>
    public static int PriceRank(string? tierId)
    {
        for (var i = 0; i < PriceTiers.Count; i++)
        {
            if (PriceTiers[i].Id == tierId)
            {
                return i;
            }
        }

        return -1;
    }
}
