using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Tests.Unit.Seeker;

/// <summary>
/// The search sheet (Airbnb's "Where? / When? / What?": one question open at a time) and the words its rows and the
/// results' pill show. A wrong rule here searches the wrong job or town, or loses the job the household picked.
/// </summary>
public class SearchSheetTests
{
    private static AppText English()
    {
        var text = new AppText();
        text.Use(AppLanguage.English);
        return text;
    }

    [Fact]
    public void Opens_OnTheCurrentSearch_WhatFirst_LaunchTownPicked()
    {
        var sheet = SearchSheet.From(new SeekerAnswers { CategoryId = "aircon", ServiceId = "aircon-cleaning", UrgencyId = "today" });

        sheet.Open.Should().Be(SearchStep.What);
        (sheet.CategoryId, sheet.ServiceId, sheet.TownId, sheet.UrgencyId).Should().Be(("aircon", "aircon-cleaning", "angono", "today"));
        sheet.CanSearch.Should().BeTrue();
        SearchSheet.From(new SeekerAnswers()).CanSearch.Should().BeFalse("a search needs words or a trade");
    }

    [Fact]
    public void Picking_MovesToTheNextQuestion()
    {
        var sheet = SearchSheet.From(new SeekerAnswers());

        sheet.PickTrade("aircon");
        sheet.Open.Should().Be(SearchStep.What, "the trade's job tiles show under it");
        sheet.PickJob("aircon-cleaning");
        sheet.Open.Should().Be(SearchStep.Where);
        sheet.PickTown("taytay");
        sheet.Open.Should().Be(SearchStep.When);
        sheet.PickUrgency("today");
        sheet.Open.Should().Be(SearchStep.When);
        sheet.OpenStep(SearchStep.What);
        sheet.Open.Should().Be(SearchStep.What);
        (sheet.CategoryId, sheet.ServiceId, sheet.TownId, sheet.UrgencyId).Should().Be(("aircon", "aircon-cleaning", "taytay", "today"));
    }

    [Fact]
    public void TypingAndTrades_ReplaceEachOther()
    {
        var sheet = SearchSheet.From(new SeekerAnswers { CategoryId = "aircon", ServiceId = "aircon-cleaning" });

        sheet.Type("  tubero ");
        (sheet.Words, sheet.CategoryId, sheet.ServiceId).Should().Be(("tubero", null, null));

        sheet.Type("   ");
        sheet.Words.Should().BeNull();

        sheet.Type("linis");
        sheet.PickTrade("aircon");
        (sheet.Words, sheet.CategoryId).Should().Be(((string?)null, (string?)"aircon"));
    }

    [Fact]
    public void ClearingTheWordsOfAnOthersSearch_AsksWhatAgain()
    {
        var others = SearchSheet.From(new SeekerAnswers { CategoryId = ReferenceData.OtherTradeId, ServiceId = ReferenceData.OtherTradeId });

        others.Type("");

        others.CategoryId.Should().BeNull("\"Others\" with no words searches nothing");
        others.ServiceId.Should().BeNull();

        var aircon = SearchSheet.From(new SeekerAnswers { CategoryId = "aircon", ServiceId = "aircon-cleaning" });
        aircon.Type("");
        aircon.CategoryId.Should().Be("aircon", "a tapped trade stays while the box is empty");
    }

    [Fact]
    public void ATradeTile_AlwaysMeansAllItsJobs_UntilAJobTileIsPicked()
    {
        var current = new SeekerAnswers { CategoryId = "aircon", ServiceId = "aircon-cleaning" };
        var opened = SearchSheet.From(current);
        opened.ServiceId.Should().Be("aircon-cleaning", "the sheet opens on the current search");
        opened.NeedsJobPick(current).Should().BeFalse("nothing changed");

        var same = SearchSheet.From(current);
        same.PickTrade("aircon");
        same.ServiceId.Should().BeNull("a trade tile means all its jobs, whatever was searched before");
        same.NeedsJobPick(current).Should().BeTrue();

        same.PickJob("aircon-repair");
        same.ServiceId.Should().Be("aircon-repair");
        same.PickJob(SearchSheet.AllJobs);
        same.ServiceId.Should().BeNull();

        var afterWords = new SeekerAnswers { CategoryId = "aircon", SearchText = "aircon linis" };
        var picked = SearchSheet.From(afterWords);
        picked.PickTrade("aircon");
        picked.NeedsJobPick(afterWords).Should().BeTrue("a tapped trade replaces the words searched before");

        var typed = SearchSheet.From(current);
        typed.Type("tubero");
        typed.NeedsJobPick(current).Should().BeFalse("typed words are searched as words");
    }

    [Fact]
    public void JobTiles_AllFirst_ThenTheTradesJobs()
    {
        var tiles = English().JobChoices("aircon");

        tiles[0].Id.Should().Be(SearchSheet.AllJobs);
        tiles[0].Label.Should().Be("All Aircon Technician services");
        tiles.Skip(1).Select(t => t.Id).Should().Equal(ReferenceData.ServicesOf("aircon").Select(s => s.Id));
    }

    [Fact]
    public void ClearAll_EmptySearchInTheLaunchTown()
    {
        var sheet = SearchSheet.From(new SeekerAnswers { CategoryId = "aircon", CityId = "taytay", UrgencyId = "today", SearchText = "x" });
        sheet.PickTown("antipolo");

        sheet.Clear();

        (sheet.Words, sheet.CategoryId, sheet.ServiceId, sheet.UrgencyId).Should().Be(((string?)null, (string?)null, (string?)null, (string?)null));
        sheet.TownId.Should().Be("angono");
        sheet.Open.Should().Be(SearchStep.What);
        sheet.CanSearch.Should().BeFalse();
    }

    [Fact]
    public void RowsAndPill_SayTheAnswers()
    {
        var text = English();

        text.What("  tubero ", "aircon", null).Should().Be("tubero");
        text.What(null, "aircon", "aircon-cleaning").Should().Be("Aircon cleaning");
        text.What(null, "aircon", null).Should().Be("Aircon Technician");
        text.What(null, null, null).Should().BeNull();
        text.Where("angono").Should().Be("Angono");
        text.Where("nowhere").Should().BeNull();
        text.When(null).Should().Be("Any time");
        text.When("today").Should().Be("Today, it's urgent");

        text.SummaryTitle("Aircon cleaning", "Angono").Should().Be("Aircon cleaning in Angono");
        text.SummaryTitle(null, "Angono").Should().Be("Angono");
        text.SummaryTitle(null, null).Should().Be("Start your search");
        text.SummaryNote(null, null).Should().Be("Any time · Any budget");
        text.SummaryNote(null, ReferenceData.Unsure).Should().Be("Any time · Any budget");
        text.SummaryNote("today", "500-1000").Should().StartWith("Today, it's urgent · ₱500");
    }
}
