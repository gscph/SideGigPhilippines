using SideGigPh.Contracts;

namespace SideGigPh.Tests.Unit.Moderation;

/// <summary>
/// The blocked-words check (<see cref="BlockedWords"/>) against its samples (<c>blocked-words-samples.txt</c>): every
/// <c>[block]</c> line is caught, including the spellings people use to get around a filter, and no <c>[allow]</c>
/// line is (everyday words that look alike: puto, Taytay, leche flan, may alagang hayop ka ba). Every label, town and
/// barangay in the app config passes too, so a new job name in SQL can't be refused as rude.
/// </summary>
public class BlockedWordsTests
{
    private static readonly Lazy<Dictionary<string, List<string>>> Samples = new(() =>
    {
        var sections = new Dictionary<string, List<string>> { ["[block]"] = [], ["[allow]"] = [] };
        string? section = null;
        foreach (var line in File.ReadLines(Path.Combine(AppContext.BaseDirectory, "Unit", "Moderation", "blocked-words-samples.txt")))
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith('#'))
            {
                continue;
            }

            if (sections.ContainsKey(trimmed))
            {
                section = trimmed;
                continue;
            }

            sections[section!].Add(line);
        }

        return sections;
    });

    [Fact]
    public void Every_block_sample_is_caught()
    {
        Samples.Value["[block]"].Should().NotBeEmpty();
        Samples.Value["[block]"].Where(sample => !BlockedWords.Contains(sample)).Should().BeEmpty();
    }

    [Fact]
    public void No_allow_sample_is_caught()
    {
        Samples.Value["[allow]"].Should().NotBeEmpty();
        Samples.Value["[allow]"].Where(BlockedWords.Contains).Should().BeEmpty();
    }

    [Theory]
    [InlineData("put@")]
    [InlineData("g@g0")]
    [InlineData("b0b0")]
    [InlineData("vovo")]
    [InlineData("kup@l")]
    [InlineData("8==D")]
    [InlineData("../..")]
    [InlineData("p u t a")]
    [InlineData("Putang ina mo")]
    public void The_workarounds_people_try_are_caught(string text) => BlockedWords.Contains(text).Should().BeTrue();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Linis ng aircon sa Taytay, ₱1,500")]
    [InlineData("Puto at leche flan para sa handaan")]
    [InlineData("Barado ang poso negro")]
    public void Normal_service_talk_passes(string? text) => BlockedWords.Contains(text).Should().BeFalse();

    [Fact]
    public void No_label_town_or_barangay_in_the_app_config_is_caught()
    {
        var sql = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Sql", "004-config.sql"));
        var labels = System.Text.RegularExpressions.Regex.Matches(sql, "N'([^']*)'").Select(m => m.Groups[1].Value).Distinct().ToList();

        labels.Should().NotBeEmpty();
        labels.Where(BlockedWords.Contains).Should().BeEmpty();
    }
}
