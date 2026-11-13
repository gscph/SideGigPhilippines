using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services;

/// <summary>
/// The answer lists the app offers, in English and Taglish. The lists that may be stale (trades and jobs, towns,
/// price tiers, experience, urgency, income goals) come from the server's <c>GET config</c>, loaded once when the app
/// opens (<see cref="AppConfigService"/>) and read here by every page; until then, and offline on a first launch,
/// <see cref="BuiltInConfig"/> is used. The ids the code depends on are constants here. Taglish uses the words
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

    /// <summary>The top experience answer; Seekers see it highlighted in the brand color.</summary>
    public const string TopExperienceId = "10-plus";

    /// <summary>
    /// The one area (province) we serve for the MVP. The interviews never ask for it: it follows from the town
    /// (<see cref="AreaOf"/>), and listings and search still carry it so more areas can return without a schema change.
    /// </summary>
    public const string ServiceAreaId = "rizal";

    /// <summary>Ends the id of every trade's "Others / Wala sa nabanggit" job (<c>aircon-other</c>), always its last job.</summary>
    public const string OtherJobSuffix = "-other";

    private static readonly LocalizedText OtherJob = new("Others", "Wala sa nabanggit");

    // The two answers that live with the code (they mean "no tier"), added after the server's price scale.
    private static readonly CatalogOption UnsureOption = new(Unsure, new("Not sure yet", "Di ko pa alam"), "🤔");
    private static readonly CatalogOption QuoteOption = new(Quote, new("Depends on the job", "Depende sa trabaho"), "💬");

    // --- The config in use ------------------------------------------------------

    /// <summary>The config every page reads: the server's once loaded, else <see cref="BuiltInConfig"/>.</summary>
    public static AppConfig Config => Current.Config;

    /// <summary>The trades in display order.</summary>
    public static IReadOnlyList<CatalogOption> Categories => Current.Config.Categories;

    /// <summary>Each trade's jobs, most-requested first, "Others" last. Use <see cref="ServicesOf"/> for a trade that may be gone.</summary>
    public static IReadOnlyDictionary<string, IReadOnlyList<CatalogOption>> ServicesByCategory => Current.Config.ServicesByCategory;

    /// <summary>Every town we serve (A–Z), all in <see cref="ServiceAreaId"/>. A saved town no longer here is dropped on load (<see cref="OnboardingCleanup"/>).</summary>
    public static IReadOnlyList<CatalogOption> Cities => Current.Config.Cities;

    public static IReadOnlyList<CatalogOption> Urgencies => Current.Config.Urgencies;

    public static IReadOnlyList<CatalogOption> Experience => Current.Config.Experience;

    /// <summary>
    /// The one price scale both sides answer on, cheapest first, so a Seeker's budget and a Professional's rate can be
    /// compared. <see cref="CatalogOption.Amount"/> is a typical job price inside the tier.
    /// </summary>
    public static IReadOnlyList<CatalogOption> PriceTiers => Current.Config.PriceTiers;

    /// <summary><see cref="PriceTiers"/> + "Not sure yet".</summary>
    public static IReadOnlyList<CatalogOption> BudgetOptions => Current.BudgetOptions;

    /// <summary><see cref="PriceTiers"/> + "Depends on the job".</summary>
    public static IReadOnlyList<CatalogOption> RateOptions => Current.RateOptions;

    /// <summary>Monthly earnings goals. <see cref="CatalogOption.Amount"/> is pesos per month.</summary>
    public static IReadOnlyList<CatalogOption> IncomeGoals => Current.Config.IncomeGoals;

    /// <summary>
    /// Switches every page to <paramref name="config"/> (the server's, or the copy saved on the phone). Ignored unless
    /// <see cref="CanUse"/>. Called once, while the app opens.
    /// </summary>
    public static void UseConfig(AppConfig config)
    {
        if (CanUse(config))
        {
            Volatile.Write(ref _current, new Loaded(config));
        }
    }

    /// <summary>
    /// A config the app can run on: every list has answers, every trade has jobs, and the ids the code depends on
    /// (<see cref="TopExperienceId"/>, the urgencies) are there. Anything else is never used or saved.
    /// </summary>
    public static bool CanUse(AppConfig config) =>
        config.Categories.Count > 0
        && config.Categories.All(category => config.ServicesByCategory.TryGetValue(category.Id, out var services) && services.Count > 0)
        && config.Cities.Count > 0 && config.PriceTiers.Count > 0 && config.IncomeGoals.Count > 0
        && Find(config.Experience, TopExperienceId) is not null
        && new[] { UrgencyToday, UrgencyThisWeek, UrgencyCanvassing }.All(id => Find(config.Urgencies, id) is not null);

    /// <summary>A trade's jobs, or none for a trade that isn't offered (retired).</summary>
    public static IReadOnlyList<CatalogOption> ServicesOf(string? categoryId) =>
        categoryId is not null && ServicesByCategory.TryGetValue(categoryId, out var services) ? services : [];

    /// <summary>The "Others" job of a trade: picking it lets the person type what they need (kept for analytics only).</summary>
    public static bool IsOtherJob(string? serviceId) => serviceId?.EndsWith(OtherJobSuffix, StringComparison.Ordinal) == true;

    // --- The copy built into the app ------------------------------------------------

    // SideGigPh.AzureSQL/Sql/004-config.sql as it was when this app was built, used on the very first launch before
    // the server's config loads. Ids are stored in saved answers, listings and chats: never rename them, only their
    // labels. Within each trade, jobs are listed most-requested first, and every trade ends with "Others" so nobody
    // is forced into a wrong pick. Towns are A–Z; price tiers cheapest first. Keep this in step with 004-config.sql
    // (an integration test compares them).
    public static AppConfig BuiltInConfig { get; } = new(
    [
        new("aircon", new("Aircon Technician", "Aircon Technician"), "❄️", new("Cleaning, repair, installation", "Linis, repair, pakabit")),
        new("plumbing", new("Plumber", "Tubero"), "🚰", new("Clogs, leaks, faucets, CR", "Barado, tagas, gripo, CR")),
        new("electrical", new("Electrician", "Electrician"), "⚡", new("Power, breaker, outlets, lights", "Kuryente, breaker, saksakan, ilaw")),
        new("roofing", new("Roofing & Gutter", "Roofing & Gutter"), "🏠", new("Roof leaks, gutters, waterproofing", "Tulo sa bubong, yero, alulod, waterproofing")),
        new("carpentry", new("Carpentry", "Karpintero"), "🔨", new("Furniture, doors, cabinets", "Custom furniture, pinto, cabinet")),
        new("welder", new("Welder", "Welder"), "🔥", new("Gates, grills, railings", "Gate, grills, rehas, pa-weld")),
        new("masonry_tiling", new("Masonry", "Mason"), "🧱", new("Tiles, cement work, walls", "Tiles, semento, palitada, pader")),
        new("car_mechanic", new("Car Mechanic", "Mekaniko"), "🚗", new("Suspension, engine, electronics", "Suspension, makina, electronics")),
        new("errand_runner", new("Errands & Delivery", "Errand Runner / PasaBuy"), "🛵", new("Grocery, bills payment, documents", "Pa-grocery, pabayad ng bills, hatid-papeles")),
        new("solar_cleaning", new("Solar Panel Technician", "Solar Panel Technician"), "☀️", new("Panel cleaning and check-up", "Linis at check-up ng solar panel")),
    ],
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
            new("aircon-other", OtherJob, "➕"),
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
            new("plumbing-other", OtherJob, "➕"),
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
            new("electrical-other", OtherJob, "➕"),
        ],
        ["roofing"] =
        [
            new("roofing-leak", new("Roof leak repair", "Tulo sa bubong"), "🌧️"),
            new("roofing-gutter", new("Gutter cleaning or repair", "Linis o repair ng alulod"), "🍂"),
            new("roofing-sheet", new("Roof sheet replacement", "Palit ng yero"), "🔩"),
            new("roofing-waterproofing", new("Waterproofing (roof deck, walls)", "Waterproofing ng roof deck o pader"), "🛡️"),
            new("roofing-paint", new("Roof painting", "Pintura ng bubong"), "🎨"),
            new("roofing-other", OtherJob, "➕"),
        ],
        ["carpentry"] =
        [
            new("carpentry-furniture", new("Furniture repair", "Repair ng furniture"), "🪑"),
            new("carpentry-door-window", new("Door or window repair", "Repair ng pinto o bintana"), "🚪"),
            new("carpentry-cabinet", new("Custom cabinets or shelves", "Pagawa ng cabinet o estante"), "🗄️"),
            new("carpentry-ceiling-roof", new("Ceiling repair", "Repair ng kisame"), "🏠"),
            new("carpentry-partition", new("Partition or drywall", "Partition o drywall"), "🧱"),
            new("carpentry-other", OtherJob, "➕"),
        ],
        ["welder"] =
        [
            new("welder-repair", new("Welding repair", "Pa-weld ng sira"), "🔥"),
            new("welder-gate", new("Gate (new or repair)", "Pagawa o repair ng gate"), "🚧"),
            new("welder-grills", new("Window grills", "Grills sa bintana"), "🪟"),
            new("welder-railings", new("Stair or balcony railings", "Rehas sa hagdan o balcony"), "🪜"),
            new("welder-frame", new("Steel frame or trusses", "Bakal na frame o trusses"), "🏗️"),
            new("welder-other", OtherJob, "➕"),
        ],
        ["masonry_tiling"] =
        [
            new("masonry-tiles", new("Tile installation or replacement", "Pakabit o palit ng tiles"), "🔲"),
            new("masonry-cement", new("Cement work / plastering", "Semento / palitada"), "🪣"),
            new("masonry-crack", new("Crack repair (wall or floor)", "Ayos ng bitak sa pader o sahig"), "🔨"),
            new("masonry-wall", new("Wall or fence (hollow blocks)", "Pader o bakod (hollow blocks)"), "🧱"),
            new("masonry-cr-renovation", new("CR / bathroom renovation", "Renovate ng CR / banyo"), "🚿"),
            new("masonry-other", OtherJob, "➕"),
        ],
        ["car_mechanic"] =
        [
            new("mechanic-suspension", new("Suspension", "Suspension"), "🛞"),
            new("mechanic-engine", new("Engine", "Makina"), "⚙️"),
            new("mechanic-electronics", new("Electronics", "Electronics"), "🔋"),
            new("mechanic-other", OtherJob, "➕"),
        ],
        ["errand_runner"] =
        [
            new("errand-grocery", new("Grocery or market run", "Pa-grocery o pa-palengke"), "🛒"),
            new("errand-bills", new("Bills payment", "Pabayad ng bills"), "🧾"),
            new("errand-pasabuy", new("PasaBuy (buy and deliver)", "PasaBuy"), "🛍️"),
            new("errand-documents", new("Document pickup or delivery", "Pakuha o pahatid ng papeles"), "📄"),
            new("errand-parcel", new("Parcel pickup or drop-off", "Pa-pickup o pa-drop off ng parcel"), "📦"),
            new("errand-other", OtherJob, "➕"),
        ],
        ["solar_cleaning"] =
        [
            new("solar-cleaning", new("Solar panel cleaning", "Pa-linis ng solar panel"), "🧽"),
            new("solar-checkup", new("Solar check-up / maintenance", "Check-up o maintenance ng solar"), "📋"),
            new("solar-inverter", new("Inverter or battery check", "Check ng inverter o battery"), "🔋"),
            new("solar-other", OtherJob, "➕"),
        ],
    },
    Cities:
    [
        Town("angono", "Angono"),
        Town("antipolo", "Antipolo"),
        Town("taytay", "Taytay"),
    ],
    PriceTiers:
    [
        new("under-500", new("Below ₱500", "Mas mababa sa ₱500"), Amount: 400),
        new("500-1000", new("₱500 – ₱1,000", "₱500 – ₱1,000"), Amount: 750),
        new("1000-2000", new("₱1,000 – ₱2,000", "₱1,000 – ₱2,000"), Amount: 1500),
        new("2000-5000", new("₱2,000 – ₱5,000", "₱2,000 – ₱5,000"), Amount: 3500),
        new("5000-up", new("₱5,000 and up", "₱5,000 pataas"), Amount: 5000),
    ],
    Experience:
    [
        new("under-1", new("Less than 1 year", "Wala pang 1 taon"), "🌱"),
        new("1-3", new("1–3 years", "1–3 taon"), "🧰"),
        new("3-10", new("3–10 years", "3–10 taon"), "🛠️"),
        new(TopExperienceId, new("10+ years", "Mahigit 10 taon"), "🏆"),
    ],
    Urgencies:
    [
        new(UrgencyToday, new("Today, it's urgent", "Ngayon na, urgent"), "⚡"),
        new(UrgencyThisWeek, new("This week", "Ngayong linggo"), "📅"),
        new(UrgencyCanvassing, new("Just comparing prices", "Nagca-canvass lang"), "🔍"),
    ],
    IncomeGoals:
    [
        new("5000", new("₱5,000", "₱5,000"), "🎯", Amount: 5000),
        new("10000", new("₱10,000", "₱10,000"), "🎯", Amount: 10000),
        new("20000", new("₱20,000", "₱20,000"), "🎯", Amount: 20000),
        new("30000-up", new("₱30,000 or more", "₱30,000 pataas"), "🎯", Amount: 30000),
    ]);

    // After BuiltInConfig: static fields start in the order they are written.
    private static Loaded _current = new(BuiltInConfig);

    private static Loaded Current => Volatile.Read(ref _current);

    public static CatalogOption? Find(IEnumerable<CatalogOption> options, string? id) =>
        id is null ? null : options.FirstOrDefault(o => o.Id == id);

    public static CatalogOption? FindService(string? id) =>
        Find(ServicesByCategory.Values.SelectMany(services => services), id);

    /// <summary>The area a town belongs to (<see cref="ServiceAreaId"/>), or null for a town we don't serve.</summary>
    public static string? AreaOf(string? cityId) =>
        Find(Cities, cityId) is null ? null : ServiceAreaId;

    // Place names are the same in English and Taglish.
    private static CatalogOption Town(string id, string name) => new(id, new(name, name));

    /// <summary>A config with the lists built from it, swapped in as one so a page never sees half of each.</summary>
    private sealed class Loaded(AppConfig config)
    {
        public AppConfig Config { get; } = config;

        public IReadOnlyList<CatalogOption> BudgetOptions { get; } = [.. config.PriceTiers, UnsureOption];

        public IReadOnlyList<CatalogOption> RateOptions { get; } = [.. config.PriceTiers, QuoteOption];
    }
}
