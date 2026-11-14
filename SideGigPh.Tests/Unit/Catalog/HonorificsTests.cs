using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Tests.Unit.Catalog;

/// <summary>
/// Tarbaho calls workers Manong, Kuya, Ate or Manang, never "laborers". The honorific must fit the trade, the
/// rotation must always include the women's honorifics, and the copy must read right in both languages.
/// </summary>
public class HonorificsTests
{
    [Theory]
    [InlineData("roofing", Honorifics.Manong)]
    [InlineData("carpentry", Honorifics.Manong)]
    [InlineData("welder", Honorifics.Manong)]
    [InlineData("masonry_tiling", Honorifics.Manong)]
    [InlineData("aircon", Honorifics.Kuya)]
    [InlineData("plumbing", Honorifics.Kuya)]
    [InlineData("electrical", Honorifics.Kuya)]
    [InlineData("errand_runner", Honorifics.Ate)]
    [InlineData("a_new_trade", Honorifics.Kuya)]
    [InlineData(null, Honorifics.Kuya)]
    public void For_FitsTheTrade(string? categoryId, string expected) =>
        Honorifics.For(categoryId).Should().Be(expected);

    [Fact]
    public void Rotation_IncludesAtesAndManangs() =>
        Honorifics.All.Should().BeEquivalentTo([Honorifics.Manong, Honorifics.Kuya, Honorifics.Ate, Honorifics.Manang]);

    [Fact]
    public void Random_IsAlwaysOneOfTheFour() =>
        Enumerable.Range(0, 50).Select(_ => Honorifics.Random()).Should().OnlyContain(word => Honorifics.All.Contains(word));

    [Fact]
    public void Copy_EnglishUsesThePlural_TaglishTheWord()
    {
        var text = new AppText();
        text.FormatHonorific(Copy.SeekerTagline, Honorifics.Manang, englishPlural: true).Should().Be("Find Manangs nearby");
        text.FormatHonorific(Copy.SearchPlaceholder, Honorifics.Manong).Should().Be("Find a Manong…");

        text.FormatHonorific(Copy.ResultsEmpty, Honorifics.Ate, englishPlural: true).Should().Be("No Ates for this job here yet… soon!");

        text.Use(AppLanguage.Taglish);
        text.FormatHonorific(Copy.SeekerTagline, Honorifics.Ate, englishPlural: true).Should().Be("May Ate para diyan");
        text.FormatHonorific(Copy.ResultsEmpty, Honorifics.Kuya, englishPlural: true).Should().Be("Wala pang Kuya dito… soon!");
    }
}
