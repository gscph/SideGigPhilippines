using SideGigPh.Contracts;
using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services;

/// <summary>
/// The answer lists the app offers, in English and Taglish. The lists that may be stale (trades and jobs, towns,
/// price guides, experience, urgency, income goals) come from the server's <c>GET config</c>, loaded once when the app
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

    /// <summary>
    /// The "Others / Wala sa nabanggit" tile after the trades in both interviews: someone whose kind of work we don't
    /// offer types it, and it is sent for analytics (<c>OtherJobNotes</c> with this as the trade). Never saved as a trade.
    /// </summary>
    public const string OtherTradeId = "other";

    public const string UrgencyToday = "today";
    public const string UrgencyThisWeek = "this-week";
    public const string UrgencyCanvassing = "canvassing";

    /// <summary>The Seeker's last question (<see cref="FindByChoices"/>): see workers now, or post what they need.</summary>
    public const string FindBySearch = "search";
    public const string FindByPost = "post";

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

    // The answers that live with the code: "no budget", "no set price", and the three rates a worker can charge
    // (a point of each job's price guide, PriceLevels in SideGigPh.Contracts, shared with the server's ranking).
    public static readonly CatalogOption UnsureOption = new(Unsure, new("Not sure yet", "Di ko pa alam"), AppImages.Unsure);
    public static readonly CatalogOption QuoteOption = new(Quote, new("Depends on the job", "Depende sa trabaho"), AppImages.Quote);

    /// <summary>The rates a Skilled Worker can pick, cheapest first: each job's lowest, average or above-average price.</summary>
    public static IReadOnlyList<CatalogOption> RateLevels { get; } =
    [
        new(PriceLevels.Minimum, new("Minimum price", "Pinakamababang presyo"), AppImages.For(PriceLevels.Minimum)),
        new(PriceLevels.Average, new("Average price", "Karaniwang presyo"), AppImages.For(PriceLevels.Average)),
        new(PriceLevels.AboveAverage, new("Above average", "Mas mataas sa karaniwan"), AppImages.For(PriceLevels.AboveAverage)),
    ];

    /// <summary>
    /// How a household wants to find a worker, the Seeker interview's last question. Lives with the code (it picks a
    /// page, not a search filter), never in <c>004-config.sql</c>.
    /// </summary>
    public static IReadOnlyList<CatalogOption> FindByChoices { get; } =
    [
        new(FindBySearch, new("See workers now", "Tingnan ang mga gagawa"), AppImages.SearchNow,
            new("Pick one near you and message them", "Pumili ng malapit at i-message")),
        new(FindByPost, new("Post what I need", "I-post ang ipapagawa ko"), AppImages.PostRequest,
            new("Workers near you send you their price", "Ang mga malapit ang magpapadala ng presyo nila")),
    ];

    private static readonly CatalogOption OtherTradeOption = new(
        OtherTradeId, OtherJob, AppImages.More, new("Something else? Type it", "Iba pa? I-type mo"));

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

    /// <summary>The trades + "Others", the tiles of both interviews' trade question.</summary>
    public static IReadOnlyList<CatalogOption> TradeChoices => Current.TradeChoices;

    /// <summary>
    /// The old one-scale price tiers, cheapest first, kept only to read listings and budgets saved by app versions
    /// before price guides. <see cref="CatalogOption.Amount"/> is a typical job price inside the tier. New answers use
    /// <see cref="Prices"/> (<see cref="Pricing"/>).
    /// </summary>
    public static IReadOnlyList<CatalogOption> PriceTiers => Current.Config.PriceTiers;

    /// <summary>Each job's price guide, by job id. Read through <see cref="Pricing"/>.</summary>
    public static IReadOnlyDictionary<string, PriceGuide> Prices => Current.Config.Prices;

    /// <summary>The NCR daily minimum wage (pesos): no job's lowest price and no Seeker budget goes under it.</summary>
    public static int MinimumWage => Current.Config.MinimumWage;

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
    /// A config the app can run on: every list has answers, every trade has jobs and every job a price guide (lowest at
    /// or above the minimum wage, then average, then above average), no trade takes the "Others" tile's id, and the ids
    /// the code depends on (<see cref="TopExperienceId"/>, the urgencies) are there, and every picture is one of the
    /// app's (<see cref="AppImages"/>). Anything else (e.g. a copy saved before price guides, or with emoji for
    /// pictures) is never used or saved.
    /// </summary>
    public static bool CanUse(AppConfig config) =>
        config.Prices is not null  // a copy saved before price guides reads back with none
        && config.Categories.Count > 0
        && config.Categories.All(category => category.Id != OtherTradeId
            && config.ServicesByCategory.TryGetValue(category.Id, out var services) && services.Count > 0
            && services.All(service => config.Prices.TryGetValue(service.Id, out var price) && IsSound(price, config.MinimumWage)))
        && config.MinimumWage > 0
        && config.Cities.Count > 0 && config.PriceTiers.Count > 0 && config.IncomeGoals.Count > 0
        && Find(config.Experience, TopExperienceId) is not null
        && new[] { UrgencyToday, UrgencyThisWeek, UrgencyCanvassing }.All(id => Find(config.Urgencies, id) is not null)
        && config.Categories.Concat(config.ServicesByCategory.Values.SelectMany(services => services))
            .Concat(config.Experience).Concat(config.Urgencies).All(option => AppImages.IsImage(option.Icon));

    private static bool IsSound(PriceGuide price, int minimumWage) =>
        price.Min >= minimumWage && price.Min <= price.Average && price.Average <= price.High;

    /// <summary>
    /// A trade's jobs, or none for a trade that isn't offered (retired). The "Others" trade (<see cref="OtherTradeId"/>)
    /// has one job, itself: what the person typed says the rest.
    /// </summary>
    public static IReadOnlyList<CatalogOption> ServicesOf(string? categoryId) =>
        categoryId == OtherTradeId ? Current.OtherTradeJobs
        : categoryId is not null && ServicesByCategory.TryGetValue(categoryId, out var services) ? services : [];

    /// <summary>
    /// An "Others" answer: a trade's last job (<c>…-other</c>) or the "Others" trade itself. Picking it lets the person
    /// type what they need; the text is sent for analytics and matched against the other side's (search).
    /// </summary>
    public static bool IsOtherJob(string? serviceId) => OtherWork.IsOther(serviceId);

    /// <summary>A trade, or the "Others" tile, by id; null for a trade we don't offer.</summary>
    public static CatalogOption? FindTrade(string? id) => Find(TradeChoices, id);

    // --- The copy built into the app ------------------------------------------------

    // SideGigPh.AzureSQL/Sql/004-config.sql as it was when this app was built, used on the very first launch before
    // the server's config loads. Ids are stored in saved answers, listings and chats: never rename them, only their
    // labels. Within each trade, jobs are listed most-requested first, and every trade ends with "Others" so nobody
    // is forced into a wrong pick. Towns are A–Z; price tiers cheapest first; prices are pesos, labor only (lowest,
    // average, above average; the lowest never under the NCR minimum wage, Wage Order NCR-28). It may lag behind
    // 004-config.sql; every id here must still exist there (an integration test checks).
    public static AppConfig BuiltInConfig { get; } = new(
    [
        new("aircon", new("Aircon Technician", "Aircon Technician"), AppImages.For("aircon"), new("Cleaning, repair, installation", "Linis, repair, pakabit")),
        new("construction", new("Construction", "Construction"), AppImages.For("construction"), new("Foreman, plumber, mason, welder, roofer", "Foreman, tubero, mason, welder, bubong")),
        new("errand_runner", new("Errands", "Errands"), AppImages.For("errand_runner"), new("House cleaning, grocery, bills, gardening", "Linis ng bahay, pa-grocery, bills, halaman")),
        new("it", new("IT", "IT"), AppImages.For("it"), new("Computers, software, networks, AI", "Computer, software, network, AI")),
        new("electrical", new("Electrician", "Electrician"), AppImages.For("electrical"), new("Power, breaker, outlets, lights", "Kuryente, breaker, saksakan, ilaw")),
        new("carpentry", new("Carpentry", "Karpintero"), AppImages.For("carpentry"), new("Furniture, doors, cabinets", "Custom furniture, pinto, cabinet")),
        new("masonry_tiling", new("Masonry", "Mason"), AppImages.For("masonry_tiling"), new("Tiles, cement work, walls", "Tiles, semento, palitada, pader")),
        new("car_mechanic", new("Car Mechanic", "Mekaniko"), AppImages.For("car_mechanic"), new("Suspension, engine, electronics", "Suspension, makina, electronics")),
        new("solar_cleaning", new("Solar Panel Technician", "Solar Panel Technician"), AppImages.For("solar_cleaning"), new("Panel cleaning and check-up", "Linis at check-up ng solar panel")),
    ],
    new Dictionary<string, IReadOnlyList<CatalogOption>>
    {
        ["aircon"] =
        [
            new("aircon-cleaning", new("Aircon cleaning", "Pa-linis ng aircon"), AppImages.For("aircon-cleaning")),
            new("aircon-repair", new("Aircon not cooling / repair", "Hindi lumalamig / pa-repair ng aircon"), AppImages.For("aircon-repair")),
            new("aircon-leak", new("Aircon leaking water", "Tumutulo ang aircon"), AppImages.For("aircon-leak")),
            new("aircon-freon", new("Freon recharge", "Pa-freon ng aircon"), AppImages.For("aircon-freon")),
            new("aircon-install", new("Aircon installation (window / split-type)", "Pakabit ng aircon (window / split-type)"), AppImages.For("aircon-install")),
            new("aircon-relocate", new("Aircon removal or transfer", "Pabaklas o palipat ng aircon"), AppImages.For("aircon-relocate")),
            new("aircon-pms", new("Aircon PMS / check-up", "PMS / check-up ng aircon"), AppImages.For("aircon-pms")),
            new("aircon-other", OtherJob, AppImages.For("aircon-other")),
        ],
        ["construction"] =
        [
            new("construction-foreman", new("Foreman / maestro", "Foreman / maestro"), AppImages.For("construction-foreman")),
            new("construction-carpentry", new("Carpentry", "Karpintero"), AppImages.For("construction-carpentry")),
            new("construction-plumbing", new("Plumbing (clogs, leaks, pipes, CR)", "Tubero (barado, tagas, lababo, CR)"), AppImages.For("construction-plumbing")),
            new("construction-masonry", new("Masonry (hollow blocks, cement)", "Mason (hollow blocks, semento)"), AppImages.For("construction-masonry")),
            new("construction-tiles", new("Tile setting", "Pakabit ng tiles"), AppImages.For("construction-tiles")),
            new("construction-welding", new("Welding (gates, grills, railings)", "Welder (gate, grills, rehas)"), AppImages.For("construction-welding")),
            new("construction-roofing", new("Roofing & gutter (leaks, roof sheets)", "Bubong at alulod (tulo, yero)"), AppImages.For("construction-roofing")),
            new("construction-steelman", new("Steelman (rebar work)", "Steelman (bakal / rebar)"), AppImages.For("construction-steelman")),
            new("construction-helper", new("Construction helper", "Helper / peon"), AppImages.For("construction-helper")),
            new("construction-other", OtherJob, AppImages.For("construction-other")),
        ],
        ["electrical"] =
        [
            new("electrical-no-power", new("No power / breaker keeps tripping", "Walang kuryente / nagta-trip ang breaker"), AppImages.For("electrical-no-power")),
            new("electrical-outlet", new("Outlet or switch repair", "Sirang outlet o switch"), AppImages.For("electrical-outlet")),
            new("electrical-lights", new("Lights or ceiling fan", "Pakabit ng ilaw o ceiling fan"), AppImages.For("electrical-lights")),
            new("electrical-wiring", new("Wiring or rewiring", "Wiring o rewiring"), AppImages.For("electrical-wiring")),
            new("electrical-breaker-panel", new("Breaker or panel box", "Palit ng breaker o panel box"), AppImages.For("electrical-breaker-panel")),
            new("electrical-meter", new("Meralco meter / new line", "Pakabit ng metro / bagong linya (Meralco)"), AppImages.For("electrical-meter")),
            new("electrical-cctv", new("CCTV installation", "Pakabit ng CCTV"), AppImages.For("electrical-cctv")),
            new("electrical-other", OtherJob, AppImages.For("electrical-other")),
        ],
        ["carpentry"] =
        [
            new("carpentry-furniture", new("Furniture repair", "Repair ng furniture"), AppImages.For("carpentry-furniture")),
            new("carpentry-door-window", new("Door or window repair", "Repair ng pinto o bintana"), AppImages.For("carpentry-door-window")),
            new("carpentry-cabinet", new("Custom cabinets or shelves", "Pagawa ng cabinet o estante"), AppImages.For("carpentry-cabinet")),
            new("carpentry-ceiling-roof", new("Ceiling repair", "Repair ng kisame"), AppImages.For("carpentry-ceiling-roof")),
            new("carpentry-partition", new("Partition or drywall", "Partition o drywall"), AppImages.For("carpentry-partition")),
            new("carpentry-other", OtherJob, AppImages.For("carpentry-other")),
        ],
        ["masonry_tiling"] =
        [
            new("masonry-tiles", new("Tile installation or replacement", "Pakabit o palit ng tiles"), AppImages.For("masonry-tiles")),
            new("masonry-cement", new("Cement work / plastering", "Semento / palitada"), AppImages.For("masonry-cement")),
            new("masonry-crack", new("Crack repair (wall or floor)", "Ayos ng bitak sa pader o sahig"), AppImages.For("masonry-crack")),
            new("masonry-wall", new("Wall or fence (hollow blocks)", "Pader o bakod (hollow blocks)"), AppImages.For("masonry-wall")),
            new("masonry-cr-renovation", new("CR / bathroom renovation", "Renovate ng CR / banyo"), AppImages.For("masonry-cr-renovation")),
            new("masonry-other", OtherJob, AppImages.For("masonry-other")),
        ],
        ["car_mechanic"] =
        [
            new("mechanic-suspension", new("Suspension", "Suspension"), AppImages.For("mechanic-suspension")),
            new("mechanic-engine", new("Engine", "Makina"), AppImages.For("mechanic-engine")),
            new("mechanic-electronics", new("Electronics", "Electronics"), AppImages.For("mechanic-electronics")),
            new("mechanic-other", OtherJob, AppImages.For("mechanic-other")),
        ],
        ["errand_runner"] =
        [
            new("errand-house-cleaning", new("House cleaning", "Linis ng bahay"), AppImages.For("errand-house-cleaning")),
            new("errand-grocery", new("Grocery or market run", "Pa-grocery o pa-palengke"), AppImages.For("errand-grocery")),
            new("errand-bills", new("Bills payment", "Pabayad ng bills"), AppImages.For("errand-bills")),
            new("errand-pasabuy", new("PasaBuy (buy and deliver)", "PasaBuy"), AppImages.For("errand-pasabuy")),
            new("errand-documents", new("Document pickup or delivery", "Pakuha o pahatid ng papeles"), AppImages.For("errand-documents")),
            new("errand-parcel", new("Parcel pickup or drop-off", "Pa-pickup o pa-drop off ng parcel"), AppImages.For("errand-parcel")),
            new("errand-car-cleaning", new("Car cleaning", "Linis ng kotse"), AppImages.For("errand-car-cleaning")),
            new("errand-gardening", new("Gardening", "Pag-aalaga ng halaman / garden"), AppImages.For("errand-gardening")),
            new("errand-other", OtherJob, AppImages.For("errand-other")),
        ],
        ["it"] =
        [
            new("it-software", new("Software", "Software"), AppImages.For("it-software")),
            new("it-hardware", new("Hardware", "Hardware"), AppImages.For("it-hardware")),
            new("it-cybersecurity", new("Cybersecurity", "Cybersecurity"), AppImages.For("it-cybersecurity")),
            new("it-ai", new("Artificial Intelligence", "Artificial Intelligence (AI)"), AppImages.For("it-ai")),
            new("it-enterprise", new("Enterprise Solutions", "Enterprise Solutions"), AppImages.For("it-enterprise")),
            new("it-other", OtherJob, AppImages.For("it-other")),
        ],
        ["solar_cleaning"] =
        [
            new("solar-cleaning", new("Solar panel cleaning", "Pa-linis ng solar panel"), AppImages.For("solar-cleaning")),
            new("solar-checkup", new("Solar check-up / maintenance", "Check-up o maintenance ng solar"), AppImages.For("solar-checkup")),
            new("solar-inverter", new("Inverter or battery check", "Check ng inverter o battery"), AppImages.For("solar-inverter")),
            new("solar-other", OtherJob, AppImages.For("solar-other")),
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
        new("under-1", new("Less than 1 year", "Wala pang 1 taon"), AppImages.For("under-1")),
        new("1-3", new("1–3 years", "1–3 taon"), AppImages.For("1-3")),
        new("3-10", new("3–10 years", "3–10 taon"), AppImages.For("3-10")),
        new(TopExperienceId, new("10+ years", "Mahigit 10 taon"), AppImages.For(TopExperienceId)),
    ],
    Urgencies:
    [
        new(UrgencyToday, new("Today, it's urgent", "Ngayon na, urgent"), AppImages.For(UrgencyToday)),
        new(UrgencyThisWeek, new("This week", "Ngayong linggo"), AppImages.For(UrgencyThisWeek)),
        new(UrgencyCanvassing, new("Just comparing prices", "Nagca-canvass lang"), AppImages.For(UrgencyCanvassing)),
    ],
    IncomeGoals:
    [
        new("5000", new("₱5,000", "₱5,000"), Amount: 5000),
        new("10000", new("₱10,000", "₱10,000"), Amount: 10000),
        new("20000", new("₱20,000", "₱20,000"), Amount: 20000),
        new("30000-up", new("₱30,000 or more", "₱30,000 pataas"), Amount: 30000),
    ],
    Prices: new Dictionary<string, PriceGuide>
    {
        ["aircon-cleaning"] = new(755, 1000, 1500),
        ["aircon-repair"] = new(755, 1500, 2500),
        ["aircon-leak"] = new(755, 1000, 1500),
        ["aircon-freon"] = new(1200, 2000, 3000),
        ["aircon-install"] = new(2500, 3500, 5000),
        ["aircon-relocate"] = new(1500, 2500, 4000),
        ["aircon-pms"] = new(755, 1200, 1800),
        ["aircon-other"] = new(755, 1200, 2000),
        ["construction-foreman"] = new(1200, 1800, 2500),
        ["construction-carpentry"] = new(755, 1200, 2000),
        ["construction-plumbing"] = new(755, 1200, 2000),
        ["construction-masonry"] = new(755, 1200, 2000),
        ["construction-tiles"] = new(1000, 2000, 3500),
        ["construction-welding"] = new(755, 1200, 2000),
        ["construction-roofing"] = new(1000, 2000, 3500),
        ["construction-steelman"] = new(755, 1100, 1500),
        ["construction-helper"] = new(755, 850, 1000),
        ["construction-other"] = new(755, 1200, 2000),
        ["electrical-no-power"] = new(755, 1000, 1500),
        ["electrical-outlet"] = new(755, 900, 1300),
        ["electrical-lights"] = new(755, 1000, 1500),
        ["electrical-wiring"] = new(1500, 3000, 5000),
        ["electrical-breaker-panel"] = new(1000, 1800, 3000),
        ["electrical-meter"] = new(1500, 2500, 4000),
        ["electrical-cctv"] = new(1500, 2500, 4000),
        ["electrical-other"] = new(755, 1200, 2000),
        ["carpentry-furniture"] = new(755, 1200, 2000),
        ["carpentry-door-window"] = new(755, 1200, 2000),
        ["carpentry-cabinet"] = new(2500, 5000, 8000),
        ["carpentry-ceiling-roof"] = new(1000, 2000, 3500),
        ["carpentry-partition"] = new(2000, 4000, 7000),
        ["carpentry-other"] = new(1000, 1800, 3000),
        ["masonry-tiles"] = new(1500, 3000, 5000),
        ["masonry-cement"] = new(1000, 2000, 3500),
        ["masonry-crack"] = new(755, 1500, 2500),
        ["masonry-wall"] = new(2000, 4000, 7000),
        ["masonry-cr-renovation"] = new(5000, 10000, 18000),
        ["masonry-other"] = new(1000, 2000, 3500),
        ["mechanic-suspension"] = new(1000, 2000, 3500),
        ["mechanic-engine"] = new(1500, 3000, 5000),
        ["mechanic-electronics"] = new(1000, 2000, 3500),
        ["mechanic-other"] = new(1000, 2000, 3500),
        ["errand-house-cleaning"] = new(755, 1000, 1500),
        ["errand-grocery"] = new(755, 900, 1200),
        ["errand-bills"] = new(755, 850, 1000),
        ["errand-pasabuy"] = new(755, 900, 1200),
        ["errand-documents"] = new(755, 900, 1200),
        ["errand-parcel"] = new(755, 850, 1000),
        ["errand-car-cleaning"] = new(755, 900, 1200),
        ["errand-gardening"] = new(755, 1000, 1500),
        ["errand-other"] = new(755, 900, 1200),
        ["it-software"] = new(755, 1200, 2000),
        ["it-hardware"] = new(755, 1500, 2500),
        ["it-cybersecurity"] = new(1000, 2000, 3500),
        ["it-ai"] = new(1500, 3000, 5000),
        ["it-enterprise"] = new(3000, 6000, 10000),
        ["it-other"] = new(755, 1500, 2500),
        ["solar-cleaning"] = new(1000, 2000, 3500),
        ["solar-checkup"] = new(1000, 1800, 3000),
        ["solar-inverter"] = new(1000, 2000, 3500),
        ["solar-other"] = new(1000, 2000, 3500),
    },
    MinimumWage: 755);

    // After BuiltInConfig: static fields start in the order they are written.
    private static Loaded _current = new(BuiltInConfig);

    private static Loaded Current => Volatile.Read(ref _current);

    public static CatalogOption? Find(IEnumerable<CatalogOption> options, string? id) =>
        id is null ? null : options.FirstOrDefault(o => o.Id == id);

    public static CatalogOption? FindService(string? id) =>
        id == OtherTradeId ? OtherTradeOption : Find(ServicesByCategory.Values.SelectMany(services => services), id);

    /// <summary>The area a town belongs to (<see cref="ServiceAreaId"/>), or null for a town we don't serve.</summary>
    public static string? AreaOf(string? cityId) =>
        Find(Cities, cityId) is null ? null : ServiceAreaId;

    // Place names are the same in English and Taglish.
    private static CatalogOption Town(string id, string name) => new(id, new(name, name));

    /// <summary>A config with the lists built from it, swapped in as one so a page never sees half of each.</summary>
    private sealed class Loaded(AppConfig config)
    {
        public AppConfig Config { get; } = config;

        public IReadOnlyList<CatalogOption> TradeChoices { get; } = [.. config.Categories, OtherTradeOption];

        public IReadOnlyList<CatalogOption> OtherTradeJobs { get; } = [OtherTradeOption];
    }
}
