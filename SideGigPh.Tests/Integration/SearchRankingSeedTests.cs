using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SideGigPh.AzureSQL;
using SideGigPh.Contracts;
using SideGigPh.Maui.Services.Search;
using SideGigPh.Tests.Integration.Support;

namespace SideGigPh.Tests.Integration;

/// <summary>
/// <c>006-seed-search-ranking.sql</c> is how people test search ranking by hand
/// (docs/search-ranking-test-plan.md). These tests run the seed on a database of its own and check every scenario
/// in that plan gives the order the plan promises, through the real store and ranker. If one fails after a weight
/// changes, update the plan and the expected order together. The typed searches the plan uses are checked too.
/// </summary>
[Trait("Category", "Integration")]
public sealed class SearchRankingSeedTests(SearchRankingSeedTests.SeededDatabase seeded) : IClassFixture<SearchRankingSeedTests.SeededDatabase>
{
    [IntegrationFact]
    public async Task S1_TheJobIsAHardFilter()
    {
        (await InTown("construction", "construction-plumbing")).Should().Equal("S1 Tubero at Welder", "S1 Tubero Specialist");
        (await InTown("construction", "construction-welding")).Should().Equal("S1 Welder Specialist", "S1 Tubero at Welder");
        (await Search("construction", "construction-steelman")).Hits.Should().BeEmpty("nobody lists steelman work");
        (await InTown("construction", null)).Should().HaveCount(3, "a whole-trade search shows every construction worker");
    }

    [IntegrationFact]
    public async Task S2_YourTownFirst_NearbyAfter_UnservedTownsNever()
    {
        var taytay = await Search("electrical", "electrical-no-power");
        Names(taytay, inTown: true).Should().Equal("S2 Taytay at Antipolo", "S2 Taytay Baguhan");
        Names(taytay, inTown: false).Should().Equal("S2 Antipolo Star", "S2 Angono Kuryente");

        var antipolo = await Search("electrical", "electrical-no-power", city: "antipolo");
        Names(antipolo, inTown: true).Should().Equal("S2 Antipolo Star", "S2 Taytay at Antipolo");
        Names(antipolo, inTown: false).Should().Equal("S2 Angono Kuryente", "S2 Taytay Baguhan");
    }

    [IntegrationFact]
    public async Task S3_ManyGoodReviewsBeatOneLuckyReview_BadReviewsSinkBelowNone() =>
        (await InTown("carpentry", "carpentry-furniture")).Should().Equal(
            "S3 Steady 4.8 (40 reviews)", "S3 Lucky 5.0 (1 review)", "S3 Bago (no reviews)", "S3 Poor 2.0 (10 reviews)");

    [IntegrationFact]
    public async Task S4_RepliesWin_UnansweredChatsSink_CallsAndFreshChatsDontCount() =>
        (await InTown("errand_runner", "errand-house-cleaning")).Should().Equal(
            "S4 Mabilis Sumagot (10 min)",
            "S4 Matagal Sumagot (5 hrs)",
            "S4 Bago (no chats yet)",            // these three score the same (A–Z):
            "S4 Kaka-message (2 hrs ago)",       // a chat under a day old isn't held against them yet
            "S4 Tawag Lang (calls only)",        // a Call may have been answered on the phone
            "S4 Hindi Sumasagot (1 of 10)");

    [IntegrationFact]
    public async Task S5_RecentlyActiveFirst() =>
        (await InTown("masonry_tiling", "masonry-tiles")).Should().Equal(
            "S5 Active Today", "S5 Active 1 Week Ago", "S5 Active 1 Month Ago", "S5 Active 3 Months Ago");

    [IntegrationFact]
    public async Task S6_LongerOnTheAppFirst() =>
        (await InTown("car_mechanic", "mechanic-engine")).Should().Equal(
            "S6 Joined 1 Year Ago", "S6 Joined 3 Months Ago", "S6 Joined 3 Days Ago");

    [IntegrationFact]
    public async Task S7_InBudgetFirst_ALittleOverBeforeQuote_FarOverLast_OldTiersStillRead()
    {
        // Roof leak repair: ₱1,000 lowest, ₱2,000 average, ₱3,500 above average. Old tiers: ₱750 and ₱5,000.
        (await InTown("roofing", "roofing-leak")).Should().Equal(
            "S7 Average Price", "S7 Lowest Price", "S7 Old App, Cheap Tier",   // ₱1,000–₱2,000 (Tipid): all fit (A–Z)
            "S7 Depends on the Job", "S7 Above Average", "S7 Old App, Top Tier");
        (await InTown("roofing", "roofing-leak", budget: "2000-3500")).Should().Equal(
            "S7 Above Average", "S7 Average Price", "S7 Lowest Price", "S7 Old App, Cheap Tier",
            "S7 Old App, Top Tier", "S7 Depends on the Job");                    // ₱5,000 is 43% over: still beats no price
        (await InTown("roofing", "roofing-leak", budget: "1000-1500")).Should().Equal(
            "S7 Lowest Price", "S7 Old App, Cheap Tier", "S7 Average Price",
            "S7 Depends on the Job", "S7 Above Average", "S7 Old App, Top Tier"); // the last two are far over: no points (A–Z)
        (await InTown("roofing", "roofing-leak", budget: "unsure")).Should().Equal(
            "S7 Above Average", "S7 Average Price", "S7 Depends on the Job", "S7 Lowest Price", "S7 Old App, Cheap Tier", "S7 Old App, Top Tier");
    }

    [IntegrationFact]
    public async Task S8_MoreExperienceFirst() =>
        (await InTown("solar_cleaning", "solar-cleaning")).Should().Equal(
            "S8 10+ Years", "S8 3 to 10 Years", "S8 1 to 3 Years", "S8 Under 1 Year");

    [IntegrationFact]
    public async Task S9_AllTogether_PagedFourAtATime()
    {
        var all = await Search("aircon", "aircon-cleaning");
        Names(all, inTown: true).Should().Equal(
            "S9 Mang Jun Aircon",            // ≈96: great at everything
            "S9 Mahal Aircon (top price)",   // ≈85: ₱1,500 is 50% over the ₱1,000 top (10 of 20) but 20 great reviews, fast, 10+ years
            "S9 Kuya Ben Cool",              // ≈81: lowest price, good reviews, replies most chats
            "S9 Tulog Aircon (inactive)",    // ≈69: 30 good reviews and 10+ years outweigh 45 days away and 10 ignored chats
            "S9 Quote Aircon",               // ≈66
            "S9 Bagong Aircon Tech",         // ≈63: new, fits budget, neutral priors
            "S9 RJ Aircon (2 reviews)");     // ≈62: above-average price, little experience
        Names(all, inTown: false).Should().Equal("S9 Antipolo Aircon Pro", "S9 Angono Aircon");

        var firstPage = await Search("aircon", "aircon-cleaning", take: Paging.Search);
        firstPage.Hits.Should().HaveCount(4);
        (firstPage.InTownCount, firstPage.OtherTownsCount).Should().Be((7, 2));
        (await InTown("aircon", "aircon-install")).Should().Equal("S9 Install Lang", "S9 Mahal Aircon (top price)");
    }

    [Theory]
    [InlineData("tagas tubo", "construction", "construction-plumbing")]
    [InlineData("barado lababo", "construction", "construction-plumbing")]
    [InlineData("pa-weld gate", "construction", "construction-welding")]
    [InlineData("steelman rebar", "construction", "construction-steelman")]
    [InlineData("walang kuryente", "electrical", "electrical-no-power")]
    [InlineData("repair furniture", "carpentry", "carpentry-furniture")]
    [InlineData("linis ng bahay", "errand_runner", "errand-house-cleaning")]
    [InlineData("pa-grocery", "errand_runner", "errand-grocery")]
    [InlineData("pakabit tiles", "masonry_tiling", "masonry-tiles")]
    [InlineData("makina kotse", "car_mechanic", "mechanic-engine")]
    [InlineData("tulo sa bubong", "roofing", "roofing-leak")]
    [InlineData("linis solar panel", "solar_cleaning", "solar-cleaning")]
    [InlineData("linis aircon", "aircon", "aircon-cleaning")]
    [InlineData("pakabit aircon", "aircon", "aircon-install")]
    [InlineData("hinang rehas", "construction", "construction-welding")]
    public void ThePlansTypedSearches_FindTheRightTradeAndJob(string typed, string category, string? service)
    {
        var intent = SearchIntentParser.Parse(typed);

        (intent.CategoryId, intent.ServiceId).Should().Be((category, service));
    }

    /// <summary>The budget a Seeker gets first for a job (the plan's default): "Tipid", its lowest to average price, e.g. <c>755-1000</c>.</summary>
    private const string Tipid = "tipid";

    private Task<List<string>> InTown(string category, string? service, string? budget = Tipid) =>
        Search(category, service, budget: budget).ContinueWith(t => Names(t.Result, inTown: true));

    private async Task<SearchPageDto> Search(string category, string? service, string city = "taytay", string? budget = Tipid, int take = ProfessionalStore.MaxSearchPage)
    {
        using var scope = seeded.Services.CreateScope();
        if (budget == Tipid)
        {
            budget = service is null ? null : await TipidOf(scope.ServiceProvider.GetRequiredService<SideGigPhDb>(), service);
        }

        return await scope.ServiceProvider.GetRequiredService<ProfessionalStore>()
            .SearchPageAsync(category, "rizal", new RankRequest(service, city, budget), 0, take, new HashSet<string>(), CancellationToken.None);
    }

    private static async Task<string> TipidOf(SideGigPhDb db, string serviceId)
    {
        var job = await db.Services.AsNoTracking().SingleAsync(s => s.Id == serviceId);
        return BudgetRange.Format(job.PriceMin!.Value, job.PriceAverage!.Value);
    }

    private static List<string> Names(SearchPageDto page, bool inTown) =>
        [.. (inTown ? page.Hits.Take(page.InTownCount) : page.Hits.Skip(page.InTownCount)).Select(h => h.Listing.DisplayName)];

    /// <summary>A database of its own with only the schema, the config and the search-ranking seed, made on first use.</summary>
    public sealed class SeededDatabase : IDisposable
    {
        private readonly Lazy<(TestDatabase Db, ServiceProvider Services)> _built = new(() =>
        {
            var db = new TestDatabase();
            db.RunScript("006-seed-search-ranking.sql");
            db.RunScript("006-seed-search-ranking.sql");  // twice: the plan says it's safe to run again
            var services = new ServiceCollection()
                .AddSideGigPhSql(db.ConnectionString, new PhotoLinks("https://example/photos/"))
                .BuildServiceProvider();
            return (db, services);
        });

        public IServiceProvider Services => _built.Value.Services;

        public void Dispose()
        {
            if (_built.IsValueCreated)
            {
                _built.Value.Services.Dispose();
                _built.Value.Db.Dispose();
            }
        }
    }
}
