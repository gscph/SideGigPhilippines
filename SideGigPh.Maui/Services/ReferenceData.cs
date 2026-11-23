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

    // The answer that lives with the code: "no budget".
    public static readonly CatalogOption UnsureOption = new(Unsure, new("Not sure yet", "Di ko pa alam"), AppImages.Unsure);

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

    /// <summary>
    /// The launch's town (server-set, <c>default-town</c> in <c>004-config.sql</c>), pre-selected wherever a person picks
    /// a town; the others stay selectable. Null when none is set or it is not a town we serve.
    /// </summary>
    public static string? DefaultTownId => Find(Cities, Current.Config.DefaultTownId)?.Id;

    /// <summary>The barangays we serve in <paramref name="cityId"/> (A–Z), none for a town without a list. Picking one is always optional.</summary>
    public static IReadOnlyList<CatalogOption> BarangaysOf(string? cityId) =>
        cityId is not null && Current.Config.BarangaysByTown?.TryGetValue(cityId, out var barangays) == true ? barangays : [];

    /// <summary>A barangay we serve in <paramref name="cityId"/>, or null (none picked, retired, or of another town).</summary>
    public static CatalogOption? FindBarangay(string? cityId, string? barangayId) => Find(BarangaysOf(cityId), barangayId);

    public static IReadOnlyList<CatalogOption> Urgencies => Current.Config.Urgencies;

    public static IReadOnlyList<CatalogOption> Experience => Current.Config.Experience;

    /// <summary>The trades + "Others", the tiles of both interviews' trade question.</summary>
    public static IReadOnlyList<CatalogOption> TradeChoices => Current.TradeChoices;

    /// <summary>Each job's price guide, by job id. Read through <see cref="Pricing"/>.</summary>
    public static IReadOnlyDictionary<string, PriceGuide> Prices => Current.Config.Prices;

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
    /// A config the app can run on: every list has answers, every trade has jobs and every job a price guide (more than
    /// ₱0, low to average to high), no trade takes the "Others" tile's id, and the ids
    /// the code depends on (<see cref="TopExperienceId"/>, the urgencies) are there, and every picture is one of the
    /// app's (<see cref="AppImages"/>). Anything else (e.g. a copy saved before price guides, or with emoji for
    /// pictures) is never used or saved.
    /// </summary>
    public static bool CanUse(AppConfig config) =>
        config.Prices is not null  // a copy saved before price guides reads back with none
        && config.Categories.Count > 0
        && config.Categories.All(category => category.Id != OtherTradeId
            && config.ServicesByCategory.TryGetValue(category.Id, out var services) && services.Count > 0
            && services.All(service => config.Prices.TryGetValue(service.Id, out var price) && IsSound(price)))
        && config.Cities.Count > 0 && config.IncomeGoals.Count > 0
        && Find(config.Experience, TopExperienceId) is not null
        && new[] { UrgencyToday, UrgencyThisWeek, UrgencyCanvassing }.All(id => Find(config.Urgencies, id) is not null)
        && config.Categories.Concat(config.ServicesByCategory.Values.SelectMany(services => services))
            .Concat(config.Experience).Concat(config.Urgencies).All(option => AppImages.IsImage(option.Icon));

    private static bool IsSound(PriceGuide price) =>
        price.Min > 0 && price.Min <= price.Average && price.Average <= price.High;

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
    // is forced into a wrong pick. Towns are A–Z; prices are pesos, labor only (low, average, high; only a guide). It
    // may lag behind 004-config.sql; every id here must still exist there (an integration test checks).
    public static AppConfig BuiltInConfig { get; } = new(
    [
        new("aircon", new("Aircon Technician", "Aircon Technician"), AppImages.For("aircon"), new("Cleaning, repair, installation", "Linis, repair, pakabit")),
        new("construction", new("Construction", "Construction"), AppImages.For("construction"), new("Carpenter, electrician, plumber, mason, welder", "Karpintero, electrician, tubero, mason, welder")),
        new("errand_runner", new("Errands", "Errands"), AppImages.For("errand_runner"), new("House cleaning, grocery, bills, gardening", "Linis ng bahay, pa-grocery, bills, halaman")),
        new("driver", new("Driver (On-Call)", "Driver (On-Call)"), AppImages.For("driver"), new("Family, out of town, NAIA, motorcycle", "Family driver, out of town, NAIA, motorsiklo")),
        new("car_mechanic", new("Car Mechanic", "Mekaniko"), AppImages.For("car_mechanic"), new("Suspension, engine, electronics", "Suspension, makina, electronics")),
        new("pest_control", new("Pest Control", "Pest Control"), AppImages.For("pest_control"), new("Termites, cockroaches, rats, mosquitoes", "Anay, ipis, daga, lamok, surot")),
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
            new("construction-carpentry", new("Carpentry (furniture, doors, cabinets, ceiling)", "Karpintero (furniture, pinto, cabinet, kisame)"), AppImages.For("construction-carpentry")),
            new("construction-electrical", new("Electrical (wiring, outlets, breaker, CCTV)", "Electrician (kuryente, wiring, breaker, CCTV)"), AppImages.For("construction-electrical")),
            new("construction-plumbing", new("Plumbing (clogs, leaks, pipes, CR)", "Tubero (barado, tagas, lababo, CR)"), AppImages.For("construction-plumbing")),
            new("construction-masonry", new("Masonry (hollow blocks, cement, cracks, walls)", "Mason (hollow blocks, semento, bitak, pader)"), AppImages.For("construction-masonry")),
            new("construction-tiles", new("Tile setting", "Pakabit ng tiles"), AppImages.For("construction-tiles")),
            new("construction-welding", new("Welding (gates, grills, railings)", "Welder (gate, grills, rehas)"), AppImages.For("construction-welding")),
            new("construction-roofing", new("Roofing & gutter (leaks, roof sheets)", "Bubong at alulod (tulo, yero)"), AppImages.For("construction-roofing")),
            new("construction-steelman", new("Steelman (rebar work)", "Steelman (bakal / rebar)"), AppImages.For("construction-steelman")),
            new("construction-helper", new("Construction helper", "Helper / peon"), AppImages.For("construction-helper")),
            new("construction-other", OtherJob, AppImages.For("construction-other")),
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
        ["driver"] =
        [
            new("driver-family", new("Family driver for the day", "Family driver (per araw)"), AppImages.For("driver-family")),
            new("driver-out-of-town", new("Out-of-town trip (Tagaytay, Baguio…)", "Out of town (Tagaytay, Baguio…)"), AppImages.For("driver-out-of-town")),
            new("driver-airport", new("Airport transfer (NAIA)", "Hatid-sundo sa airport (NAIA)"), AppImages.For("driver-airport")),
            new("driver-motorcycle", new("Motorcycle rider (hatid-sundo)", "Rider ng motor (hatid-sundo)"), AppImages.For("driver-motorcycle")),
            new("driver-other", OtherJob, AppImages.For("driver-other")),
        ],
        ["pest_control"] =
        [
            new("pest-termites", new("Termites", "Anay"), AppImages.For("pest-termites")),
            new("pest-cockroaches", new("Cockroaches", "Ipis"), AppImages.For("pest-cockroaches")),
            new("pest-rats", new("Rats and mice", "Daga"), AppImages.For("pest-rats")),
            new("pest-mosquitoes", new("Mosquito misting", "Pa-misting sa lamok"), AppImages.For("pest-mosquitoes")),
            new("pest-bedbugs", new("Bed bugs", "Surot"), AppImages.For("pest-bedbugs")),
            new("pest-ants", new("Ants", "Langgam"), AppImages.For("pest-ants")),
            new("pest-general", new("Whole-house treatment", "Spray sa buong bahay"), AppImages.For("pest-general")),
            new("pest-other", OtherJob, AppImages.For("pest-other")),
        ],
    },
    Cities:
    [
        Town("angono", "Angono"),
        Town("antipolo", "Antipolo"),
        Town("taytay", "Taytay"),
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
        new(UrgencyCanvassing, new("Just looking around", "Tumitingin-tingin lang"), AppImages.For(UrgencyCanvassing)),
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
        ["construction-electrical"] = new(755, 1200, 2500),
        ["construction-plumbing"] = new(755, 1200, 2000),
        ["construction-masonry"] = new(755, 1200, 2000),
        ["construction-tiles"] = new(1000, 2000, 3500),
        ["construction-welding"] = new(755, 1200, 2000),
        ["construction-roofing"] = new(1000, 2000, 3500),
        ["construction-steelman"] = new(755, 1100, 1500),
        ["construction-helper"] = new(755, 850, 1000),
        ["construction-other"] = new(755, 1200, 2000),
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
        ["driver-family"] = new(1000, 1500, 2500),
        ["driver-out-of-town"] = new(1500, 2500, 4000),
        ["driver-airport"] = new(800, 1200, 2000),
        ["driver-motorcycle"] = new(300, 600, 1000),
        ["driver-other"] = new(800, 1500, 2500),
        ["pest-termites"] = new(2500, 5000, 9000),
        ["pest-cockroaches"] = new(1200, 2000, 3500),
        ["pest-rats"] = new(1500, 2500, 4000),
        ["pest-mosquitoes"] = new(1200, 2000, 3500),
        ["pest-bedbugs"] = new(2000, 3500, 6000),
        ["pest-ants"] = new(1000, 1800, 3000),
        ["pest-general"] = new(1500, 2500, 4500),
        ["pest-other"] = new(1200, 2000, 3500),
    },
    DefaultTownId: "angono",
    BarangaysByTown: new Dictionary<string, IReadOnlyList<CatalogOption>>
    {
        ["angono"] = Barangays("angono", "Bagumbayan", "Kalayaan", "Mahabang Parang", "Poblacion Ibaba", "Poblacion Itaas",
            "San Isidro", "San Pedro", "San Roque", "San Vicente", "Santo Niño"),
        ["antipolo"] = Barangays("antipolo", "Bagong Nayon", "Beverly Hills", "Calawis", "Cupang", "Dalig", "Dela Paz",
            "Inarawan", "Mambugan", "Mayamot", "Muntingdilaw", "San Isidro", "San Jose", "San Juan", "San Luis", "San Roque",
            "Santa Cruz"),
        ["taytay"] = Barangays("taytay", "Dolores", "Muzon", "San Isidro", "San Juan", "Santa Ana"),
    });

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

    // A town's barangays as 004-config.sql names them: "{town}-{name}", lower case, no accents, dashes for spaces.
    private static IReadOnlyList<CatalogOption> Barangays(string townId, params string[] names) =>
        [.. names.Select(name => new CatalogOption(BarangayId(townId, name), new(name, name)))];

    private static string BarangayId(string townId, string name) =>
        $"{townId}-{name.ToLowerInvariant().Replace('ñ', 'n').Replace(' ', '-')}";

    /// <summary>A config with the lists built from it, swapped in as one so a page never sees half of each.</summary>
    private sealed class Loaded(AppConfig config)
    {
        public AppConfig Config { get; } = config;

        public IReadOnlyList<CatalogOption> TradeChoices { get; } = [.. config.Categories, OtherTradeOption];

        public IReadOnlyList<CatalogOption> OtherTradeJobs { get; } = [OtherTradeOption];
    }
}
