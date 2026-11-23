using SideGigPh.AzureSQL;
using SideGigPh.Contracts;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Tests.Unit.Catalog;

/// <summary>
/// The operator's Catalog page: new ids are made once from the name and never collide, the limits on names and price
/// guides, and the app showing a post's or chat's own copy of a name (from the day it was made) before today's.
/// </summary>
public class CatalogNamesTests
{
    private readonly AppText _text = new();

    [Theory]
    [InlineData("Tile & Grout Work", "tile-grout-work")]
    [InlineData("  Pintor ng Bahay ", "pintor-ng-bahay")]
    [InlineData("Señor Niño Repair", "senor-nino-repair")]
    [InlineData("!!!", "new")]
    public void AnId_IsMadeFromTheEnglishName(string name, string id) => CatalogStore.Slug(name, 40).Should().Be(id);

    [Fact]
    public void AnId_IsCutToFit_AndNeverEndsWithADash() =>
        CatalogStore.Slug("Aircon cleaning and repair for split type", 20).Should().Be("aircon-cleaning-and");

    [Fact]
    public void ANewId_NeverTakesAnIdInUse_OrReadsAsOthers()
    {
        var taken = new HashSet<string> { "painting", "painting-2" };

        CatalogStore.NewId("painting", 40, taken).Should().Be("painting-3");
        CatalogStore.NewId("plumbing-other", 40, new HashSet<string>()).Should().Be("plumbing-other-2", "an id ending in -other is an \"Others\" job");
        CatalogStore.NewId("other", 40, new HashSet<string>()).Should().Be("other-2", "\"other\" is the app's own \"Others\" trade");
        CatalogStore.NewId(new string('a', 40), 40, new HashSet<string> { new string('a', 40) }).Should().HaveLength(40).And.EndWith("-2");
    }

    [Fact]
    public void NamesAndPriceGuides_AreChecked()
    {
        CatalogRules.IsTradeName("Painter").Should().BeTrue();
        CatalogRules.IsTradeName("   ").Should().BeFalse();
        CatalogRules.IsTradeName(new string('a', CatalogRules.MaxTradeName + 1)).Should().BeFalse();
        CatalogRules.IsServiceName(new string('a', CatalogRules.MaxServiceName)).Should().BeTrue();

        CatalogRules.IsPrice(null).Should().BeFalse("the app refuses a job without a guide");
        CatalogRules.IsPrice(CatalogRules.OthersPrice).Should().BeTrue();
        CatalogRules.IsPrice(new PriceGuideDto(500, 800, 1200)).Should().BeTrue();
        CatalogRules.IsPrice(new PriceGuideDto(900, 800, 1200)).Should().BeFalse("low above average");
        CatalogRules.IsPrice(new PriceGuideDto(0, 800, 1200)).Should().BeFalse();
    }

    private static JobPost Post(LocalizedText? service = null, LocalizedText? trade = null) =>
        new(1, "aircon", "aircon-cleaning", "Madumi na ang aircon", 1500, "angono", [], IsOpen: true, Mine: false,
            OffersService: true, DistanceKm: 3, DateTimeOffset.Now, ServiceName: service, TradeName: trade);

    [Fact]
    public void APost_ShowsTheNamesItWasMadeWith_NotTodays()
    {
        var made = Post(new LocalizedText("Aircon washing", "Hugas aircon"), new LocalizedText("Aircon guy", "Aircon"));

        _text.ServiceLabel(made).Should().Be("Aircon washing");
        _text.TradeLabel(made).Should().Be("Aircon guy");
        _text.ServiceLabel(Post()).Should().NotBe("Aircon washing", "a post without its own copy shows today's name");
    }

    [Fact]
    public void AChatAndAPickedJob_ShowTheNameTheyWereMadeWith()
    {
        var saved = new LocalizedText("Aircon washing", "Hugas aircon");
        var chat = new Chat("p_s", "p", "s", "Pro", "Seeker", "aircon-cleaning", "angono", null, null,
            new ChatMessage("m", "p_s", "s", ChatMessageKind.Text, "Hi", DateTimeOffset.Now), null, null, DateTimeOffset.Now, saved);
        var job = new PickedJob(1, "aircon-cleaning", 1500, DateTimeOffset.Now, IAmTheHousehold: true, false, false, ServiceName: saved);

        _text.WhatAndWhere(chat).Should().StartWith("Aircon washing");
        _text.ServiceLabel(job).Should().Be("Aircon washing");
    }
}
