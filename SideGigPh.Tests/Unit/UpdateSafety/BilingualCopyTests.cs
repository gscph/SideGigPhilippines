using System.Reflection;
using System.Text.RegularExpressions;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Tests.Unit.UpdateSafety;

/// <summary>
/// The app is English / Taglish and many workers are not confident English readers. A text with an empty
/// version shows a blank button or title; a text whose {0} is missing in one language shows the wrong words or
/// crashes the formatter. Every string in <c>Copy</c>, the skeleton's <c>CommonCopy</c> and the answer lists is checked.
/// </summary>
public partial class BilingualCopyTests
{
    public static TheoryData<string, string, string> AllTexts()
    {
        var data = new TheoryData<string, string, string>();
        foreach (var type in new[] { typeof(Copy), typeof(CommonCopy) })
        {
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Static).Where(f => f.FieldType == typeof(LocalizedText)))
            {
                var text = (LocalizedText)field.GetValue(null)!;
                data.Add($"{type.Name}.{field.Name}", text.English, text.Taglish);
            }
        }

        return data;
    }

    public static TheoryData<string, string, string> AllAnswerLabels()
    {
        var data = new TheoryData<string, string, string>();
        var options = ReferenceData.Categories
            .Concat(ReferenceData.ServicesByCategory.Values.SelectMany(s => s))
            .Concat(ReferenceData.Cities)
            .Concat(ReferenceData.Urgencies)
            .Concat(ReferenceData.Experience)
            .Concat(ReferenceData.BudgetOptions)
            .Concat(ReferenceData.RateOptions)
            .Concat(ReferenceData.IncomeGoals)
            .DistinctBy(o => o.Id);
        foreach (var option in options)
        {
            data.Add(option.Id, option.Label.English, option.Label.Taglish);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(AllTexts))]
    public void EveryText_HasEnglishAndTaglish(string name, string english, string taglish)
    {
        english.Should().NotBeNullOrWhiteSpace($"{name} needs English");
        taglish.Should().NotBeNullOrWhiteSpace($"{name} needs Taglish");
    }

    [Theory]
    [MemberData(nameof(AllTexts))]
    public void EveryText_HasTheSamePlaceholdersInBothLanguages(string name, string english, string taglish) =>
        Placeholders(taglish).Should().BeEquivalentTo(Placeholders(english), $"{name} fills in the same values in both languages");

    [Theory]
    [MemberData(nameof(AllAnswerLabels))]
    public void EveryAnswer_HasEnglishAndTaglishLabel(string id, string english, string taglish)
    {
        english.Should().NotBeNullOrWhiteSpace($"{id} needs an English label");
        taglish.Should().NotBeNullOrWhiteSpace($"{id} needs a Taglish label");
    }

    [Fact]
    public void ThereAreTextsToCheck() => AllTexts().Count().Should().BeGreaterThan(100);

    private static string[] Placeholders(string text) =>
        Placeholder().Matches(text).Select(m => m.Value).Distinct().ToArray();

    [GeneratedRegex(@"\{\d+\}")]
    private static partial Regex Placeholder();
}
