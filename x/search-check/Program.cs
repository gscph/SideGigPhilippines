using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Dev;
using SideGigPh.Maui.Services.Search;

if (args.Length > 0 && args[0] == "--rank")
{
    if (args.Length < 5) { Console.Error.WriteLine("--rank <category> <service|-> <town> <budget>"); return 2; }
    var request = new SearchRequest(args[1], args[2] == "-" ? null : args[2], args[3], args[4]);
    var pros = await new StubProfessionalProfileRepository().SearchAsync(new ProfessionalSearchQuery(args[1], null));
    // No review data here: every pro gets the unreviewed prior, so this isolates job/town/budget/experience.
    var result = SearchRanker.Rank(pros, request, new Dictionary<string, ReviewSummary>());
    Console.WriteLine($"{request}  serviceFallback={result.ServiceFallback}");
    foreach (var (group, hits) in new[] { ("IN TOWN", result.InTown), ("OTHER", result.OtherTowns) })
        foreach (var h in hits)
            Console.WriteLine($"  {group,-8} {h.Pro.DisplayName,-30} score={h.Score,6:0.0} budget={h.FitsBudget,-5} job={h.OffersService}");
    return 0;
}

string[] queries = args.Length > 0 ? args :
[
    // expected trade / job in comments; "?" = should ask which kind of pro
    "aircon", "aircon cleaning", "Pa-linis ng AC", "aircond hindi lumalamig", "aircon leak",   // aircon / -, cleaning, cleaning, repair, leak
    "tubero", "barado lababo", "gripo", "tagas tubo", "poso negro",                             // plumbing / -, clog, -, leak, septic
    "walang kuryente", "electrician", "cctv", "ilaw",                                          // electrical / no-power, -, cctv, lights
    "tulo bubong", "alulod", "karpintero", "kisame", "gate", "pa weld",                        // roofing, carpentry, welder
    "tiles", "bitak pader", "pasabuy", "bayad bills", "solar", "hilot", "massage",
    "linis", "repair", "sira", "xyz",                                                           // ?, ?, ?, none
];
foreach (var q in queries)
{
    var i = SearchIntentParser.Parse(q);
    var trade = i.CategoryId ?? (i.OtherCategoryIds.Count > 0 ? "? ask" : "(none)");
    Console.WriteLine($"{q,-26} -> {trade,-15} {i.ServiceId ?? "-",-26} also: {string.Join(",", i.OtherCategoryIds)}");
}
return 0;
