using SideGigPh.AzureSQL;
using SideGigPh.Contracts;
using Note = SideGigPh.AzureSQL.OtherJobGrouping.Note;

namespace SideGigPh.Tests.Unit.Insights;

/// <summary>
/// How the operator site groups what people typed under "Others": the cleanup that makes texts the same, exact
/// groups, merging texts that mean the same by their (hand-made) vectors, labels, examples, counts and order.
/// </summary>
public class OtherJobGroupingTests
{
    private static readonly IReadOnlyDictionary<string, float[]> NoVectors = new Dictionary<string, float[]>();

    private static string Label(string tradeId) => tradeId == OtherWork.TradeId ? "Others" : tradeId.ToUpperInvariant();

    private static Note Seeker(string text, string trade = "construction") => new(text, Wire.Seeker, trade);

    private static Note Worker(string text, string trade = OtherWork.TradeId) => new(text, Wire.Professional, trade);

    [Theory]
    [InlineData("  Linis ng PÓOL!! ", "linis ng pool")]
    [InlineData("Pool-cleaning", "pool cleaning")]
    [InlineData("pag-aayos   ng  piano", "pag aayos ng piano")]
    [InlineData("Niño's\tsapatos", "nino s sapatos")]
    [InlineData("CCTV install (2 cams)", "cctv install 2 cams")]
    [InlineData("!!!", "!!!")]
    public void Clean_LowersDropsAccentsAndPunctuation_AndCollapsesSpaces(string typed, string cleaned) =>
        OtherJobGrouping.Clean(typed).Should().Be(cleaned);

    [Fact]
    public void Without_Vectors_TheSameTextAfterCleanup_IsOneGroup()
    {
        var groups = OtherJobGrouping.Group(
            [Seeker("Pool cleaning"), Seeker("pool-cleaning!"), Worker("linis ng pool")],
            NoVectors, Label);

        groups.Select(g => (g.Label, g.Count)).Should().Equal(("Pool cleaning", 2), ("linis ng pool", 1));
    }

    [Fact]
    public void A_Group_CountsEachSide_AndItsTrades_BiggestFirst()
    {
        var group = OtherJobGrouping.Group(
            [Seeker("piano"), Seeker("Piano"), Seeker("piano", "it"), Worker("piano")],
            NoVectors, Label).Should().ContainSingle().Subject;

        (group.Count, group.Households, group.Workers).Should().Be((4, 3, 1));
        group.Trades.Select(t => (t.Id, t.Label, t.Count)).Should().Equal(
            ("construction", "CONSTRUCTION", 2), ("it", "IT", 1), (OtherWork.TradeId, "Others", 1));
    }

    [Fact]
    public void The_Label_IsTheTextTypedMostOften()
    {
        var group = OtherJobGrouping.Group(
            [Seeker("Piano tuning"), Seeker("piano tuning"), Seeker("piano tuning"), Seeker("PIANO TUNING!")],
            NoVectors, Label).Single();

        group.Label.Should().Be("piano tuning");
    }

    [Fact]
    public void Texts_ThatMeanTheSame_AreMerged_UnderTheBiggestGroupsLabel()
    {
        var vectors = new Dictionary<string, float[]>
        {
            ["pool cleaning"] = [1f, 0f, 0f],
            ["linis ng pool"] = [0.95f, 0.1f, 0f],
            ["sliding gate"] = [0f, 1f, 0f],
        };

        var groups = OtherJobGrouping.Group(
            [Seeker("linis ng pool"), Worker("Pool cleaning"), Worker("pool cleaning"), Seeker("sliding gate")],
            vectors, Label);

        groups.Select(g => (g.Label, g.Count)).Should().Equal(("Pool cleaning", 3), ("sliding gate", 1));
        groups[0].Examples.Should().Equal("Pool cleaning", "linis ng pool");
        (groups[0].Households, groups[0].Workers).Should().Be((1, 2));
    }

    [Fact]
    public void Texts_BelowTheLine_StayApart()
    {
        // cos = 0.8, just under SameMeaning
        var vectors = new Dictionary<string, float[]> { ["pool cleaning"] = [1f, 0f], ["pool repair"] = [0.8f, 0.6f] };
        OtherJobGrouping.Cosine(vectors["pool cleaning"], vectors["pool repair"]).Should().BeLessThan(OtherJobGrouping.SameMeaning);

        OtherJobGrouping.Group([Seeker("pool cleaning"), Seeker("pool repair")], vectors, Label).Should().HaveCount(2);
    }

    [Fact]
    public void Merging_ComparesWithTheBiggestGroupOnly_SoNearTextsDontChain()
    {
        // a~b and b~c are close, a and c are not: b joins a, c stays alone.
        var vectors = new Dictionary<string, float[]>
        {
            ["a"] = [1f, 0f],
            ["b"] = [0.9f, 0.43589f],
            ["c"] = [0.62f, 0.78f],
        };
        OtherJobGrouping.Cosine(vectors["b"], vectors["c"]).Should().BeGreaterThan(OtherJobGrouping.SameMeaning);
        OtherJobGrouping.Cosine(vectors["a"], vectors["c"]).Should().BeLessThan(OtherJobGrouping.SameMeaning);

        var groups = OtherJobGrouping.Group([Seeker("a"), Seeker("a"), Seeker("b"), Seeker("c")], vectors, Label);

        groups.Select(g => (g.Label, g.Count)).Should().Equal(("a", 3), ("c", 1));
    }

    [Fact]
    public void A_Text_WithoutAVector_IsNeverMerged()
    {
        var vectors = new Dictionary<string, float[]> { ["pool cleaning"] = [1f, 0f] };

        OtherJobGrouping.Group([Seeker("pool cleaning"), Seeker("linis ng pool")], vectors, Label).Should().HaveCount(2);
    }

    [Fact]
    public void Groups_AreBiggestFirst_ThenByLabel()
    {
        var groups = OtherJobGrouping.Group(
            [Seeker("zipline"), Seeker("aquarium"), Seeker("piano"), Seeker("piano"), Seeker("mural")],
            NoVectors, Label);

        groups.Select(g => g.Label).Should().Equal("piano", "aquarium", "mural", "zipline");
    }

    [Fact]
    public void Examples_StopAtTheLimit_MostTypedFirst()
    {
        var vectors = Enumerable.Range(0, 7).ToDictionary(i => $"job {i}", _ => new[] { 1f, 0f });
        var notes = Enumerable.Range(0, 7).SelectMany(i => Enumerable.Repeat(Seeker($"job {i}"), 7 - i)).ToList();

        var group = OtherJobGrouping.Group(notes, vectors, Label).Single();

        group.Examples.Should().Equal("job 0", "job 1", "job 2", "job 3", "job 4");
        group.Count.Should().Be(28);
    }

    [Fact]
    public void Vectors_SurviveTheirBytes()
    {
        float[] vector = [0.5f, -1.25f, 3.75e-5f, 0f];

        OtherJobGrouping.FromBytes(OtherJobGrouping.ToBytes(vector)).Should().Equal(vector);
        OtherJobGrouping.ToBytes(vector).Should().HaveCount(16);
    }

    [Fact]
    public void Cosine_OfDifferentSizesOrZeros_IsZero()
    {
        OtherJobGrouping.Cosine([1f, 0f], [1f, 0f, 0f]).Should().Be(0);
        OtherJobGrouping.Cosine([0f, 0f], [1f, 0f]).Should().Be(0);
        OtherJobGrouping.Cosine([2f, 0f], [3f, 0f]).Should().BeApproximately(1, 1e-9);
    }
}
