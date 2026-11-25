using SideGigPh.Maui.Services;

namespace SideGigPh.Tests.Unit.Onboarding;

/// <summary>
/// Picking "Others" folds the tiles to that one, so the box for what they type sits under the question instead of
/// under a long list the phone's keyboard covers.
/// </summary>
public class OtherChoiceTests
{
    private static readonly IReadOnlyList<CatalogOption> Trades = ReferenceData.TradeChoices;

    [Fact]
    public void OthersPicked_ShowsOnlyThatTile()
    {
        var shown = OtherChoice.Shown(Trades, ReferenceData.OtherTradeId, showAll: false);

        shown.Should().ContainSingle().Which.Id.Should().Be(ReferenceData.OtherTradeId);
        OtherChoice.IsFolded(Trades, ReferenceData.OtherTradeId, showAll: false).Should().BeTrue();
    }

    [Fact]
    public void ShowAll_OpensTheListAgain()
    {
        OtherChoice.Shown(Trades, ReferenceData.OtherTradeId, showAll: true).Should().BeEquivalentTo(Trades);
        OtherChoice.IsFolded(Trades, ReferenceData.OtherTradeId, showAll: true).Should().BeFalse();
    }

    [Fact]
    public void AJobsOthersTile_FoldsToo()
    {
        var jobs = ReferenceData.ServicesOf(Trades[0].Id);
        var other = jobs.Single(j => ReferenceData.IsOtherJob(j.Id));

        OtherChoice.Shown(jobs, other.Id, showAll: false).Should().ContainSingle().Which.Should().Be(other);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not-in-the-list-other")]
    public void NothingOrAnUnknownOthers_ShowsEveryTile(string? picked)
    {
        OtherChoice.Shown(Trades, picked, showAll: false).Should().BeEquivalentTo(Trades);
    }

    [Fact]
    public void ARealTradePicked_ShowsEveryTile()
    {
        OtherChoice.Shown(Trades, Trades[0].Id, showAll: false).Should().BeEquivalentTo(Trades);
    }
}
