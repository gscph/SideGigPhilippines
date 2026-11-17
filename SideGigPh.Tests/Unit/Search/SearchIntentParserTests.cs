using SideGigPh.Maui.Services.Search;

namespace SideGigPh.Tests.Unit.Search;

/// <summary>
/// The home search box turns what people type, in English, Tagalog or Taglish, into a trade (and a job when it is
/// clear). If "tubero" stops finding plumbers, customers think the app is empty. When you add a word to
/// <c>TradeWords</c>, add it here too.
/// </summary>
public class SearchIntentParserTests
{
    [Theory]
    [InlineData("tubero", "construction")]
    [InlineData("barado ang lababo", "construction")]
    [InlineData("plumber", "construction")]
    [InlineData("kuryente", "electrical")]
    [InlineData("electrician po", "electrical")]
    [InlineData("hinang gate", "construction")]
    [InlineData("mekaniko", "car_mechanic")]
    [InlineData("sira kotse", "car_mechanic")]
    [InlineData("tulo sa bubong", "construction")]
    [InlineData("karpintero", "carpentry")]
    [InlineData("tiles", "masonry_tiling")]
    [InlineData("pasabuy", "errand_runner")]
    [InlineData("solar panel", "solar_cleaning")]
    [InlineData("AIRCON!!", "aircon")]
    [InlineData("airc", "aircon")] // typed only the start of the word
    [InlineData("IT", "it")] // "it" is also an English filler word: the trade's own name still finds it
    [InlineData("it", "it")]
    [InlineData("computer technician", "it")] // "technician" is in other trades' names: it can't outvote "computer"
    [InlineData("sira laptop", "it")]
    [InlineData("wifi", "it")]
    [InlineData("aircon technician", "aircon")]
    [InlineData("solar technician", "solar_cleaning")]
    public void Parse_EverydayWords_FindTheTrade(string typed, string expectedTrade) =>
        SearchIntentParser.Parse(typed).CategoryId.Should().Be(expectedTrade);

    [Theory]
    [InlineData("aircon leak", "aircon-leak")]
    [InlineData("tumutulo ang aircon", "aircon-leak")]
    [InlineData("pa-freon", "aircon-freon")]
    [InlineData("cctv", "electrical-cctv")]
    public void Parse_ClearJob_PicksThatJob(string typed, string expectedJob) =>
        SearchIntentParser.Parse(typed).ServiceId.Should().Be(expectedJob);

    [Fact]
    public void Parse_TradeOnly_StaysBroad()
    {
        // "aircon" fits every aircon job equally: show all aircon techs, not just cleaners.
        var intent = SearchIntentParser.Parse("aircon");

        intent.CategoryId.Should().Be("aircon");
        intent.ServiceId.Should().BeNull();
    }

    [Fact]
    public void Parse_WordThatFitsEveryTrade_AsksWhichKindOfWorker()
    {
        // "repair" alone could be any trade: guessing would show the wrong workers.
        var intent = SearchIntentParser.Parse("repair");

        intent.Matched.Should().BeFalse();
        intent.OtherCategoryIds.Should().NotBeEmpty("the app offers the closest trades to pick from");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("po")]          // only a filler word
    [InlineData("xyzzy qwerty")] // nothing we know
    public void Parse_NothingUseful_MatchesNothing(string? typed) =>
        SearchIntentParser.Parse(typed).Matched.Should().BeFalse();

    [Fact]
    public void Words_DropsAccentsCaseAndFillerWords() =>
        SearchText.Words("Kailangan ko po ng TUBERO sa Parañaque!").Should().Equal("tubero", "paranaque");
}
