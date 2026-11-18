using SideGigPh.Maui.Services;
using SideGigPh.Tests.TestSupport;

namespace SideGigPh.Tests.Unit.Publishing;

/// <summary>
/// Before going live a worker ticks every promise (honest work and prices, the Terms, the community rules, fair
/// offers). If one could be skipped, a worker goes live without agreeing to it; if one starts ticked, nobody read it;
/// if the Terms lose their link, the worker agrees to something they can't open.
/// </summary>
public class PublishChecklistTests
{
    [Fact]
    public void Checklist_HasTheFourPromises_InOrder_TermsLinked() =>
        PublishChecklist.Items.Select(item => (item.Id, item.LinksTerms)).Should().Equal(
            (PublishChecklist.Honest, false),
            (PublishChecklist.Terms, true),
            (PublishChecklist.Community, false),
            (PublishChecklist.FairOffers, false));

    [Fact]
    public void EveryPromise_HasEnglishAndTaglishWords() =>
        PublishChecklist.Items.Should().OnlyContain(item =>
            !string.IsNullOrWhiteSpace(item.Text.English) && !string.IsNullOrWhiteSpace(item.Text.Taglish)
            && item.Text.English != item.Text.Taglish);

    [Fact]
    public void Promises_UseServiceWords_NeverJobHireOrEmployment() =>
        PublishChecklist.Items.Select(item => item.Text.English.ToLowerInvariant())
            .Should().NotContain(text => text.Contains("job") || text.Contains("hire") || text.Contains("employ"));

    [Fact]
    public void CanContinue_OnlyWhenEveryPromiseIsTicked()
    {
        var all = PublishChecklist.Items.Select(item => item.Id).ToList();

        PublishChecklist.CanContinue([]).Should().BeFalse();
        PublishChecklist.CanContinue(all).Should().BeTrue();
        foreach (var missing in all)
        {
            PublishChecklist.CanContinue(all.Where(id => id != missing).ToList()).Should().BeFalse($"\"{missing}\" isn't ticked");
        }
    }

    [Fact]
    public void Ticks_StartEmpty_AndUntickingOneStopsTheWay()
    {
        var ticks = new PublishCommitments();
        ticks.AllTicked.Should().BeFalse("nothing starts ticked");

        foreach (var item in PublishChecklist.Items)
        {
            ticks.Set(item.Id, true);
        }

        ticks.AllTicked.Should().BeTrue();

        ticks.Set(PublishChecklist.Community, false);
        ticks.AllTicked.Should().BeFalse();
        ticks.IsTicked(PublishChecklist.Community).Should().BeFalse();
        ticks.IsTicked(PublishChecklist.Honest).Should().BeTrue();
    }

    [Fact]
    public void Ticks_IgnoreIdsNotOnTheChecklist()
    {
        var ticks = new PublishCommitments();
        ticks.Set("something-else", true);

        ticks.IsTicked("something-else").Should().BeFalse();
        ticks.AllTicked.Should().BeFalse();
    }

    [Fact]
    public async Task StartOver_ForgetsTheTicks()
    {
        var app = await FakeApp.ProAsync(TestData.ReadyDraft(), TestData.Worker());
        foreach (var item in PublishChecklist.Items)
        {
            app.Pro.Commitments.Set(item.Id, true);
        }

        app.Pro.Forget();

        app.Pro.Commitments.AllTicked.Should().BeFalse("the next person on this phone promises for themselves");
    }
}
