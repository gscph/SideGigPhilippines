using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services.Localization;
using SideGigPh.Tests.TestSupport;

namespace SideGigPh.Tests.Unit.Search;

/// <summary>
/// The words on a worker's card and in a chat header (trade, towns, price, experience, stars). Wrong words here
/// mislead customers about price or experience, so the formatting is pinned down in both languages.
/// </summary>
public class ListingTextTests
{
    private static AppText English()
    {
        var text = new AppText();
        text.Use(AppLanguage.English);
        return text;
    }

    private static readonly ProfessionalListing Card = TestData.Listing("Ana", cities: ["taytay", "angono"], services: ["aircon-cleaning", "nope"], rate: "500-1000", experience: "10-plus");

    [Fact]
    public void CardFacts_ReadNaturally()
    {
        var text = English();

        text.Trades(Card).Should().Be("Aircon Technician");
        Card.TradeOptions().Select(trade => trade.Id).Should().Equal("aircon"); // the trade badge
        text.Towns(Card).Should().Be("Taytay, Angono");
        text.JobLabels(Card).Should().Equal("Aircon cleaning"); // an unknown job id is skipped, not shown blank
        text.Rate(Card).Should().Be("₱500 – ₱1,000 per service");
        text.RateLabel(Card).Should().Be("₱500 – ₱1,000");
        text.Experience(Card).Should().Be("Exp: 10+ years");
        text.ExperienceLabel(Card).Should().Be("10+ years");
        Card.IsSeasoned().Should().BeTrue("10+ years is highlighted");
        text.Stars(new ReviewSummary(4.25, 8)).Should().Be("4.3 ★ (8)");
    }

    [Fact]
    public void CardFacts_QuoteAndNoExperience()
    {
        var text = English();
        var listing = TestData.Listing("Ben", rate: "quote", experience: null);

        text.Rate(listing).Should().Be("Price depends on the work");
        text.RateLabel(listing).Should().BeNull();
        text.Experience(listing).Should().BeNull();
        text.ExperienceLabel(listing).Should().BeNull();
        listing.IsSeasoned().Should().BeFalse();
    }

    [Fact]
    public void ChatHeader_SaysWhatWhereWhenAndBudget()
    {
        var text = English();
        var chat = TestData.ChatWith("ana", "ana");

        text.WhatAndWhere(chat).Should().Be("Aircon cleaning · Taytay");
        text.WhenAndBudget(chat).Should().Be("Today, it's urgent · Budget: ₱500 – ₱1,000");
        text.SeekerName(chat).Should().Be("Maricel D.");
    }

    [Fact]
    public void ChatHeader_MissingAnswers_AreLeftOut()
    {
        var text = English();
        var chat = TestData.ChatWith("ana", "ana") with { ServiceId = null, CityId = null, UrgencyId = null, BudgetId = null, SeekerName = " " };

        text.WhatAndWhere(chat).Should().BeEmpty();
        text.WhenAndBudget(chat).Should().BeEmpty();
        text.SeekerName(chat).Should().Be("Customer");
    }

    [Fact]
    public void TownChoices_AreTheServedTowns() =>
        English().TownChoices().Should().HaveCount(3);
}
