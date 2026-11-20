using Microsoft.Extensions.Logging.Abstractions;
using SideGigPh.Api.Moderation;

namespace SideGigPh.Tests.Unit.Moderation;

/// <summary>
/// Tarbaho's word list: English, Tagalog, Taglish and jejemon spellings are refused, everyday words that look like
/// them are not, and the outside service only decides what the list let through.
/// </summary>
public class RudeWordsTests
{
    [Theory]
    [InlineData("fuck you")]
    [InlineData("F*CK this")]
    [InlineData("you are a b1tch")]
    [InlineData("putang ina mo")]
    [InlineData("PUTANGINAMO")]
    [InlineData("tangina naman")]
    [InlineData("p0ta")]
    [InlineData("g@g0 ka")]
    [InlineData("gagooo")]
    [InlineData("gago!!")]
    [InlineData("ulol")]
    [InlineData("p u t a")]
    [InlineData("anak ng puta")]
    [InlineData("hayop ka talaga")]
    [InlineData("kantutan tayo")]
    [InlineData("send nudes")]
    [InlineData("malibog ka")]
    [InlineData("pakyu")]
    [InlineData("Ang tarantado mo")]
    [InlineData("sh1t")]
    [InlineData("fuuuuck")]
    [InlineData("pússy")]
    public void RudeText_IsCaught(string text) => RudeWords.Contains(text).Should().BeTrue();

    [Theory]
    [InlineData("Linis ng aircon, split-type, 2 units")]
    [InlineData("Masarap ang putahe mo")]
    [InlineData("May hayop sa kisame, baka daga")]
    [InlineData("Leche flan for dessert")]
    [InlineData("cockroach problem sa kusina")]
    [InlineData("Pass the potato")]
    [InlineData("Budget 1500 to 2000 pesos!")]
    [InlineData("Graduated cum laude, 10 years experience")]
    [InlineData("Need garden hoe and rake")]
    [InlineData("Kuya Pepe, salamat po!")]
    [InlineData("Assess the leak first")]
    [InlineData("Class A materials")]
    [InlineData("")]
    [InlineData(null)]
    public void EverydayText_Passes(string? text) => RudeWords.Contains(text).Should().BeFalse();

    [Fact]
    public void EveryListedWordAndStem_IsCaughtOnItsOwn()
    {
        RudeWords.Words.Where(w => !RudeWords.Contains(w)).Should().BeEmpty();
        RudeWords.Stems.Where(s => !RudeWords.Contains($"x{s}x")).Should().BeEmpty();
    }

    [Fact]
    public async Task TheWordList_RefusesWithoutAskingTheService()
    {
        var service = new CountingModerator(ModeratorVerdict.Clean);
        var moderation = new TextModeration(service, NullLogger<TextModeration>.Instance);

        (await moderation.AllowsAsync("chat message", "gago ka", default)).Should().BeFalse();

        service.Calls.Should().Be(0);
    }

    [Theory]
    [InlineData(ModeratorVerdict.Clean, true)]
    [InlineData(ModeratorVerdict.Unchecked, true)]
    [InlineData(ModeratorVerdict.Flagged, false)]
    public async Task CleanWords_GoToTheService_WhichDecides_AndFailsOpen(ModeratorVerdict verdict, bool allowed)
    {
        var service = new CountingModerator(verdict);
        var moderation = new TextModeration(service, NullLogger<TextModeration>.Instance);

        (await moderation.AllowsAsync("post", ["Sira ang gripo", null, "  ", "Tulo sa lababo"], default)).Should().Be(allowed);

        service.Calls.Should().Be(1, "all texts go in one call");
        service.LastText.Should().Be("Sira ang gripo\nTulo sa lababo");
    }

    [Fact]
    public async Task NothingWritten_IsAllowed_WithoutAsking()
    {
        var service = new CountingModerator(ModeratorVerdict.Flagged);
        (await new TextModeration(service, NullLogger<TextModeration>.Instance).AllowsAsync("name", [null, " "], default)).Should().BeTrue();
        service.Calls.Should().Be(0);
    }

    [Theory]
    [InlineData("Sexual", 2, true)]
    [InlineData("Sexual", 0, false)]
    [InlineData("Hate", 2, true)]
    [InlineData("Violence", 2, false)]
    [InlineData("Violence", 4, true)]
    [InlineData("SelfHarm", 4, true)]
    [InlineData("Other", 6, false)]
    public void Severities_BlockFromTheirLevel(string category, int severity, bool blocked) =>
        ModerationRules.Blocks(category, severity).Should().Be(blocked);

    private sealed class CountingModerator(ModeratorVerdict verdict) : ITextModerator
    {
        public int Calls { get; private set; }

        public string? LastText { get; private set; }

        public Task<ModeratorVerdict> CheckAsync(string text, CancellationToken cancellationToken)
        {
            Calls++;
            LastText = text;
            return Task.FromResult(verdict);
        }
    }
}
