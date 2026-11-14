using Microsoft.Extensions.Logging.Abstractions;
using SideGigPh.Contracts;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Api;
using SideGigPh.Maui.Services.Local;
using SideGigPh.Tests.TestSupport;

namespace SideGigPh.Tests.Unit.Catalog;

/// <summary>
/// The app's config (trades and jobs, towns, prices…) comes from the server's <c>GET config</c>, but only once per
/// launch and at most every 12 hours: the phone keeps its copy, and when that copy is old it asks with its version so an
/// unchanged config costs no download. The app must still open with a usable config when the server can't be reached
/// (the saved copy, else the built-in one), and must never switch to a broken one (an empty list or a trade without jobs
/// would leave the interview with nothing to tap).
/// These never touch <see cref="ReferenceData.UseConfig"/>, which other tests read: the service hands the config to a recorder.
/// </summary>
public class AppConfigTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 13, 8, 0, 0, TimeSpan.Zero);

    private readonly FakeConfigSource _server = new();
    private readonly FakeConfigCache _phone = new();
    private readonly FakeClock _clock = new(Now);
    private readonly List<AppConfig> _used = [];

    private RemoteConfig<AppConfig> Remote(TimeSpan? timeout = null) => new(
        _server,
        _phone,
        AppConfigService.Rules with { FetchTimeout = timeout ?? AppConfigService.Rules.FetchTimeout },
        _clock,
        NullLogger<RemoteConfig<AppConfig>>.Instance);

    private AppConfigService Service(TimeSpan? timeout = null) => new(Remote(timeout), _used.Add);

    private static AppConfig OneTrade(string id = "car_mechanic") => ReferenceData.BuiltInConfig with
    {
        Categories = [new CatalogOption(id, new("Car Mechanic", "Mekaniko"), "🚗")],
        ServicesByCategory = new Dictionary<string, IReadOnlyList<CatalogOption>>
        {
            [id] = [new("mechanic-engine", new("Engine", "Makina")), new("mechanic-other", new("Others", "Wala sa nabanggit"))],
        },
    };

    private static AppConfig NoTrades() => ReferenceData.BuiltInConfig with
    {
        Categories = [],
        ServicesByCategory = new Dictionary<string, IReadOnlyList<CatalogOption>>(),
    };

    // --- Where the config comes from ------------------------------------------

    [Fact]
    public async Task SavedCopyUnder12Hours_IsUsed_WithNoServerCall()
    {
        var saved = OneTrade("saved");
        _phone.Saved = new(saved, Now - TimeSpan.FromHours(11), "\"v1\"");
        _server.Config = OneTrade("server");

        await Service().LoadAsync();

        _used.Should().ContainSingle().Which.Should().BeSameAs(saved);
        _server.AskedWith.Should().BeEmpty("a fresh copy costs the server nothing");
    }

    [Fact]
    public async Task OldCopy_ServerUnchanged_NoDownload_AndCountsAsCheckedAgain()
    {
        var saved = OneTrade("saved");
        _phone.Saved = new(saved, Now - TimeSpan.FromHours(13), "\"v1\"");
        _server.Config = OneTrade("server");
        _server.Version = "\"v1\"";

        await Service().LoadAsync();

        _server.AskedWith.Should().Equal("\"v1\"");
        _used.Should().ContainSingle().Which.Should().BeSameAs(saved);
        _phone.Saved!.SavedAt.Should().Be(Now, "the next 12 hours need no call");
        _phone.Saved.Value.Should().BeSameAs(saved);
        _phone.Saved.Version.Should().Be("\"v1\"");
    }

    [Fact]
    public async Task OldCopy_ServerChanged_UsesAndSavesTheNewOne_WithItsVersion()
    {
        _phone.Saved = new(OneTrade("saved"), Now - TimeSpan.FromDays(3), "\"v1\"");
        var server = OneTrade("server");
        _server.Config = server;
        _server.Version = "\"v2\"";

        await Service().LoadAsync();

        _used.Should().ContainSingle().Which.Should().BeSameAs(server);
        _phone.Saved.Should().Be(new Stamped<AppConfig>(server, Now, "\"v2\""));
    }

    [Fact]
    public async Task FirstLaunch_DownloadsAndSaves()
    {
        var server = OneTrade();
        _server.Config = server;

        await Service().LoadAsync();

        _server.AskedWith.Should().Equal([null]);
        _used.Should().ContainSingle().Which.Should().BeSameAs(server);
        _phone.Saved!.Value.Should().BeSameAs(server);
    }

    [Fact]
    public async Task Offline_OldCopy_IsBetterThanNothing()
    {
        var saved = OneTrade("saved");
        _phone.Saved = new(saved, Now - TimeSpan.FromDays(30), "\"v1\"");
        _server.Unreachable = true;

        await Service().LoadAsync();

        _used.Should().ContainSingle().Which.Should().BeSameAs(saved);
        _phone.Writes.Should().Be(0);
    }

    [Fact]
    public async Task Offline_NothingSaved_KeepsTheBuiltInCopy()
    {
        _server.Unreachable = true;

        await Service().LoadAsync();

        _used.Should().BeEmpty("the app runs on ReferenceData.BuiltInConfig");
    }

    [Fact]
    public async Task SlowServer_GivesUpAfterTheTimeout_AndUsesTheOldCopy()
    {
        var saved = OneTrade("saved");
        _phone.Saved = new(saved, Now - TimeSpan.FromDays(1), "\"v1\"");
        _server.Hang = true;

        await Service(TimeSpan.FromMilliseconds(50)).LoadAsync();

        _used.Should().ContainSingle().Which.Should().BeSameAs(saved);
    }

    [Fact]
    public async Task UnusableServerConfig_IsNeitherUsedNorSaved()
    {
        var saved = OneTrade("saved");
        _phone.Saved = new(saved, Now - TimeSpan.FromDays(1), "\"v1\"");
        _server.Config = NoTrades();
        _server.Version = "\"v2\"";

        await Service().LoadAsync();

        _used.Should().ContainSingle().Which.Should().BeSameAs(saved);
        _phone.Saved!.Value.Should().BeSameAs(saved);
    }

    [Fact]
    public async Task UnusableSavedCopy_IsIgnored_AndRefetchedWithoutItsVersion()
    {
        _phone.Saved = new(NoTrades(), Now, "\"v1\"");
        var server = OneTrade();
        _server.Config = server;

        await Service().LoadAsync();

        _server.AskedWith.Should().Equal([null], "a broken copy must not be kept alive by a 304");
        _used.Should().ContainSingle().Which.Should().BeSameAs(server);
    }

    [Fact]
    public async Task LoadsOncePerLaunch_HoweverManyPagesAsk()
    {
        _server.Config = OneTrade();
        var service = Service();

        await Task.WhenAll(service.LoadAsync(), service.LoadAsync());
        await service.LoadAsync();

        _server.AskedWith.Should().HaveCount(1);
        _used.Should().HaveCount(1);
    }

    [Fact]
    public void SavedFile_RoundTrips_EveryList()
    {
        var builtIn = ReferenceData.BuiltInConfig;
        var json = System.Text.Json.JsonSerializer.Serialize(new Stamped<AppConfig>(builtIn, Now, "\"v1\""), AppConfigJsonContext.Default.StampedAppConfig);

        var read = System.Text.Json.JsonSerializer.Deserialize(json, AppConfigJsonContext.Default.StampedAppConfig)!;

        read.SavedAt.Should().Be(Now);
        read.Version.Should().Be("\"v1\"");
        read.Value.Categories.Should().Equal(builtIn.Categories);
        read.Value.ServicesByCategory.Keys.Should().BeEquivalentTo(builtIn.ServicesByCategory.Keys);
        read.Value.ServicesByCategory["aircon"].Should().Equal(builtIn.ServicesByCategory["aircon"]);
        read.Value.Cities.Should().Equal(builtIn.Cities);
        read.Value.PriceTiers.Should().Equal(builtIn.PriceTiers);
        read.Value.Experience.Should().Equal(builtIn.Experience);
        read.Value.Urgencies.Should().Equal(builtIn.Urgencies);
        read.Value.IncomeGoals.Should().Equal(builtIn.IncomeGoals);
        read.Value.Prices.Should().BeEquivalentTo(builtIn.Prices);
        read.Value.MinimumWage.Should().Be(builtIn.MinimumWage);
    }

    [Fact]
    public void CopySavedBeforePriceGuides_IsNotUsable_SoItIsFetchedAgain()
    {
        var json = System.Text.Json.JsonSerializer.Serialize(
            new Stamped<AppConfig>(ReferenceData.BuiltInConfig, Now, "\"v1\""), AppConfigJsonContext.Default.StampedAppConfig);
        var old = System.Text.Json.Nodes.JsonNode.Parse(json)!;
        old["Value"]!.AsObject().Remove("Prices");
        old["Value"]!.AsObject().Remove("MinimumWage");

        var read = System.Text.Json.JsonSerializer.Deserialize(old.ToJsonString(), AppConfigJsonContext.Default.StampedAppConfig)!;

        ReferenceData.CanUse(read.Value).Should().BeFalse();
    }

    // --- What counts as usable --------------------------------------------------

    [Fact]
    public void BuiltInCopy_IsUsable_AndIsWhatTheAppStartsWith()
    {
        ReferenceData.CanUse(ReferenceData.BuiltInConfig).Should().BeTrue();
        ReferenceData.ServicesOf("aircon").Should().NotBeEmpty();
        ReferenceData.ServicesOf("home_massage").Should().BeEmpty("a retired trade has no jobs to show, instead of crashing");
        ReferenceData.ServicesOf(null).Should().BeEmpty();
    }

    [Fact]
    public void CanUse_RefusesAnythingTheAppCantRunOn()
    {
        var builtIn = ReferenceData.BuiltInConfig;

        ReferenceData.CanUse(NoTrades()).Should().BeFalse();
        ReferenceData.CanUse(builtIn with { ServicesByCategory = new Dictionary<string, IReadOnlyList<CatalogOption>>() })
            .Should().BeFalse("a trade without jobs");
        ReferenceData.CanUse(builtIn with { Cities = [] }).Should().BeFalse();
        ReferenceData.CanUse(builtIn with { PriceTiers = [] }).Should().BeFalse();
        ReferenceData.CanUse(builtIn with { IncomeGoals = [] }).Should().BeFalse();
        ReferenceData.CanUse(builtIn with { Experience = builtIn.Experience.Where(e => e.Id != ReferenceData.TopExperienceId).ToList() })
            .Should().BeFalse("the code highlights the top experience id");
        ReferenceData.CanUse(builtIn with { Urgencies = builtIn.Urgencies.Skip(1).ToList() })
            .Should().BeFalse("the code words the first message from the urgency ids");
        ReferenceData.CanUse(builtIn with { MinimumWage = 0 }).Should().BeFalse("job prices are checked against the minimum wage");
        ReferenceData.CanUse(builtIn with { Prices = builtIn.Prices.Where(p => p.Key != "aircon-cleaning").ToDictionary() })
            .Should().BeFalse("a job without a price guide");
        ReferenceData.CanUse(builtIn with { Prices = WithPrice("aircon-cleaning", new PriceGuide(500, 1000, 1500)) })
            .Should().BeFalse("a job's lowest price is never under the minimum wage");
        ReferenceData.CanUse(builtIn with { Prices = WithPrice("aircon-cleaning", new PriceGuide(800, 1500, 1000)) })
            .Should().BeFalse("prices go up: lowest, average, above average");
        ReferenceData.CanUse(OneTrade(ReferenceData.OtherTradeId)).Should().BeFalse("\"other\" is the Others tile after the trades");

        static Dictionary<string, PriceGuide> WithPrice(string serviceId, PriceGuide price) =>
            new(ReferenceData.BuiltInConfig.Prices) { [serviceId] = price };
    }

    [Fact]
    public void BuiltInPrices_NeverUnderTheMinimumWage_AndGoUp()
    {
        var config = ReferenceData.BuiltInConfig;
        config.MinimumWage.Should().Be(755, "the NCR daily minimum wage (Wage Order NCR-28)");
        foreach (var service in config.ServicesByCategory.Values.SelectMany(s => s))
        {
            var price = config.Prices[service.Id];
            price.Min.Should().BeGreaterThanOrEqualTo(config.MinimumWage, service.Id);
            price.Average.Should().BeGreaterThanOrEqualTo(price.Min, service.Id);
            price.High.Should().BeGreaterThanOrEqualTo(price.Average, service.Id);
        }
    }

    [Fact]
    public void UseConfig_IgnoresAnUnusableConfig()
    {
        var before = ReferenceData.Config;

        ReferenceData.UseConfig(NoTrades());

        ReferenceData.Config.Should().BeSameAs(before);
    }

    // --- "Others" notes ----------------------------------------------------------

    private readonly FakeOtherJobNoteRepository _notes = new();

    private OtherJobNotes Notes() => new(_notes, NullLogger<OtherJobNotes>.Instance);

    [Fact]
    public async Task OthersNote_IsSentTrimmed_AndCutToTheServersLength()
    {
        await Notes().NoteAsync("aircon", UserRole.Seeker, "  pa-check ng " + new string('x', 200) + "  ");

        var note = _notes.Notes.Should().ContainSingle().Subject;
        note.CategoryId.Should().Be("aircon");
        note.Side.Should().Be(UserRole.Seeker);
        note.Text.Should().StartWith("pa-check ng ").And.HaveLength(OtherJobNotes.MaxText);
        OtherJobNotes.MaxText.Should().Be(AzureSQL.OtherJobNoteStore.MaxNote);
    }

    [Theory]
    [InlineData("aircon", "   ")]
    [InlineData("aircon", null)]
    [InlineData(null, "gate")]
    public async Task OthersNote_NothingTyped_SendsNothing(string? categoryId, string? text)
    {
        await Notes().NoteAsync(categoryId, UserRole.Professional, text);

        _notes.Notes.Should().BeEmpty();
    }

    [Fact]
    public async Task OthersNote_Offline_NeverReachesThePage()
    {
        _notes.Fail = true;

        var send = () => Notes().NoteAsync("welder", UserRole.Professional, "pintura ng gate");

        await send.Should().NotThrowAsync("the interview goes on whether or not the note was saved");
    }

    // --- Server answer → app model ---------------------------------------------

    [Fact]
    public void ServerConfig_MapsToTheModel_InOrder()
    {
        var dto = new ConfigDto(
            [
                new CategoryDto("plumbing", "Plumber", "Tubero", "🚰", "Clogs", null,
                    [new ServiceDto("plumbing-clog", "Clogged sink", "Barado", "🪠", new PriceGuideDto(755, 1000, 1800)), new ServiceDto("plumbing-other", "Others", "Wala sa nabanggit", "➕")]),
                new CategoryDto("welder", "Welder", "Welder", null, null, null, [new ServiceDto("welder-other", "Others", "Wala sa nabanggit", null)]),
            ],
            [new OptionDto("taytay", "Taytay", "Taytay", "📍", 0)],
            [new OptionDto("under-500", "Under ₱500", "Below ₱500", null, 400), new OptionDto("500-1000", "₱500–1,000", "₱500–1,000", null, 750)],
            [new OptionDto(ReferenceData.TopExperienceId, "10+ years", "10+ taon", null, 0)],
            [new OptionDto(ReferenceData.UrgencyToday, "Today", "Ngayon", null, 0)],
            [new OptionDto("10000", "₱10,000", "₱10,000", null, 10000)],
            MinimumWage: 755);

        var config = dto.ToModel();

        config.Categories.Select(c => c.Id).Should().Equal("plumbing", "welder");
        config.Categories[0].Label.Should().Be(new LocalizedText("Plumber", "Tubero"));
        config.Categories[0].Hint.Should().Be(new LocalizedText("Clogs", "Clogs"), "a missing translation falls back to the other language");
        config.Categories[1].Hint.Should().BeNull();
        config.ServicesByCategory["plumbing"].Select(s => s.Id).Should().Equal("plumbing-clog", "plumbing-other");
        config.Cities.Should().Equal(new CatalogOption("taytay", new("Taytay", "Taytay"), "📍"));
        config.PriceTiers.Select(p => (p.Id, p.Amount)).Should().Equal(("under-500", 400), ("500-1000", 750));
        config.IncomeGoals.Single().Amount.Should().Be(10000);
        config.Prices.Should().BeEquivalentTo(new Dictionary<string, PriceGuide> { ["plumbing-clog"] = new(755, 1000, 1800) },
            "a job the server sends without a price (an older server) has no guide");
        config.MinimumWage.Should().Be(755);
        config.Experience.Single().Id.Should().Be(ReferenceData.TopExperienceId);
        config.Urgencies.Single().Id.Should().Be(ReferenceData.UrgencyToday);
        new OtherJobNote("welder", UserRole.Professional, "x").ToRequest().Side.Should().Be(Wire.Professional);
        new OtherJobNote("welder", UserRole.Seeker, "x").ToRequest().Should().Be(new OtherJobRequest("welder", Wire.Seeker, "x"));
    }
}
