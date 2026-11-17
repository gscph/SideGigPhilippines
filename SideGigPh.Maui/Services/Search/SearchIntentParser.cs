using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services.Search;

/// <summary>
/// Understands a free-text search ("tubero barado", "aircon leak", "pa-weld gate") by matching
/// its words against every category and job label in both languages, the category hints, and
/// the everyday words people use for each trade (<see cref="TradeWords"/>).
/// The best category wins; a specific job is picked only when one clearly fits better than the
/// rest ("aircon" alone stays the whole category, "aircon leak" becomes the leak job).
/// Words that fit any trade ("sira", "repair", "install") count only half toward the trade, and
/// when no trade clearly wins ("linis", "repair") the intent has no category: the Seeker is asked
/// which kind of pro, with the closest trades offered first.
/// </summary>
public static class SearchIntentParser
{
    /// <summary>Lowest job score that counts as a real match (a typed prefix scores 0.8).</summary>
    private const double MinServiceScore = 0.7;

    /// <summary>Lowest trade score that counts: one known word, or the start of one ("airc").</summary>
    private const double MinCategoryScore = 0.8;

    /// <summary>How much a <see cref="GenericWords"/> word counts toward choosing the trade.</summary>
    private const double GenericWordWeight = 0.5;

    /// <summary>
    /// How much a word counts toward a trade when only one of its jobs has it: "karpintero" is the Carpentry trade
    /// first, a Construction job second; "kotse" is the Car Mechanic first, the Errands car cleaning second.
    /// </summary>
    private const double JobOnlyWordWeight = 0.9;

    /// <summary>At most this many "did you mean" categories.</summary>
    private const int MaxOtherCategories = 3;

    /// <summary>Job words used in every trade. They still pick the job within a trade, but can't pick the trade alone.</summary>
    private static readonly HashSet<string> GenericWords =
    [
        "repair", "fix", "broken", "sira", "sirang", "ayos", "ayusin", "install", "installation", "pakabit", "kabit",
        "palit", "replace", "replacement", "check", "checkup", "maintenance", "service", "gawa", "pagawa", "bago",
    ];

    /// <summary>Words people use for a trade that its labels don't already contain. Lower case, no accents.</summary>
    private static readonly IReadOnlyDictionary<string, string[]> TradeWords = new Dictionary<string, string[]>
    {
        ["aircon"] = ["ac", "aircond", "airconditioner", "aircondition", "airco", "split", "window", "cooling", "technician"],
        ["construction"] = ["construction", "konstruksyon", "foreman", "maestro", "kapatas", "plumber", "tubero", "tubo", "pipe", "tubig", "toilet", "sink", "bara", "baradong", "kanal", "barado", "lababo", "inidoro", "poso", "septic", "welding", "weld", "hinang", "bakal", "steel", "rebar", "steelman", "helper", "peon", "obrero", "labor", "gate", "grills"],
        ["electrical"] = ["electrician", "electric", "kuryente", "brownout", "short", "circuit", "switch", "outlet", "light", "socket"],
        ["roofing"] = ["roof", "roofer", "bubong", "tulo", "yero", "gutter", "ulan", "rain"],
        ["carpentry"] = ["carpenter", "wood", "kahoy", "furniture", "aparador", "mesa", "upuan", "door", "pinto"],
        ["masonry_tiling"] = ["mason", "masonry", "tile", "tiles", "semento", "cement", "hollow", "blocks", "bakod", "fence", "renovation", "renovate"],
        ["errand_runner"] = ["errand", "errands", "utos", "pasabuy", "rider", "deliver", "delivery", "padala", "hatid", "bili", "bilihin", "palengke", "grocery", "cleaning", "cleaner", "house", "bahay", "kasambahay", "carwash", "wash", "hugas", "garden", "gardener", "hardin", "halaman", "damo", "grass"],
        ["solar_cleaning"] = ["solar", "panel", "panels", "battery"],
        ["car_mechanic"] = ["mechanic", "mekaniko", "car", "kotse", "sasakyan", "auto", "oto", "makina", "motor", "preno", "brake", "shock", "tune", "tuneup", "kambyo"],
    };

    /// <summary>The words of the catalog in use, rebuilt when the server's config replaces the built-in one.</summary>
    private static CatalogWords? _index;

    public static SearchIntent Parse(string? text)
    {
        var words = SearchText.Words(text);
        if (words.Count == 0)
        {
            return SearchIntent.None;
        }

        var ranked = Index()
            .Select(category => (category, score: TradeScore(words, category)))
            .Where(match => match.score > 0)
            .OrderByDescending(match => match.score)
            .ToList();
        if (ranked.Count == 0)
        {
            return SearchIntent.None;
        }

        var bestScore = ranked[0].score;
        var isTie = ranked.Count > 1 && ranked[1].score >= bestScore;
        if (bestScore < MinCategoryScore || isTie)
        {
            // Not sure which trade: offer the closest ones instead of guessing.
            return new SearchIntent(null, null, ranked.Take(MaxOtherCategories + 1).Select(match => match.category.Id).ToList());
        }

        var best = ranked[0].category;
        var others = ranked.Skip(1).Take(MaxOtherCategories).Select(match => match.category.Id).ToList();
        return new SearchIntent(best.Id, BestService(best, words), others);
    }

    private static double TradeScore(IReadOnlyList<string> words, CategoryIndex category) =>
        words.Sum(word => (GenericWords.Contains(word) ? GenericWordWeight : 1) * Math.Max(
            category.OwnWords.Max(known => SearchText.Similarity(word, known)),
            JobOnlyWordWeight * category.Words.Max(known => SearchText.Similarity(word, known))));

    private static string? BestService(CategoryIndex category, IReadOnlyList<string> words)
    {
        var services = category.Services
            .Select(service => (service.Id, score: SearchText.Score(words, service.Words)))
            .OrderByDescending(match => match.score)
            .ToList();
        if (services.Count == 0 || services[0].score < MinServiceScore)
        {
            return null;
        }

        // A tie means the words fit several jobs equally ("aircon" is in every aircon job): stay broad.
        return services.Count > 1 && services[1].score >= services[0].score ? null : services[0].Id;
    }

    private static IReadOnlyList<CategoryIndex> Index()
    {
        var trades = ReferenceData.Config;
        if (Volatile.Read(ref _index) is { } cached && ReferenceEquals(cached.Trades, trades))
        {
            return cached.Categories;
        }

        var built = BuildIndex(trades);
        Volatile.Write(ref _index, new CatalogWords(trades, built));
        return built;
    }

    private static IReadOnlyList<CategoryIndex> BuildIndex(AppConfig trades) => trades.Categories
        .Select(category =>
        {
            var services = trades.ServicesByCategory.GetValueOrDefault(category.Id, [])
                .Where(service => !ReferenceData.IsOtherJob(service.Id))
                .Select(service => new ServiceIndex(service.Id, WordsOf(service.Label).ToHashSet()))
                .ToList();
            var own = WordsOf(category.Label)
                .Concat(category.Hint is { } hint ? WordsOf(hint) : [])
                .Concat(TradeWords.GetValueOrDefault(category.Id, []))
                .ToHashSet();
            var words = own.Concat(services.SelectMany(service => service.Words)).ToHashSet();
            return new CategoryIndex(category.Id, own, words, services);
        })
        .ToList();

    private static IEnumerable<string> WordsOf(LocalizedText label) =>
        SearchText.Words(label.English).Concat(SearchText.Words(label.Taglish));

    /// <summary>A trade's words: its own (label, hint, <see cref="TradeWords"/>), and those plus its jobs' labels.</summary>
    private sealed record CategoryIndex(string Id, HashSet<string> OwnWords, HashSet<string> Words, IReadOnlyList<ServiceIndex> Services);

    private sealed record ServiceIndex(string Id, HashSet<string> Words);

    private sealed record CatalogWords(AppConfig Trades, IReadOnlyList<CategoryIndex> Categories);
}
