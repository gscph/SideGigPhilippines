using SideGigPh.Maui.Services.Search;

namespace SideGigPh.Maui.Services.Localization;

/// <summary>The towns as choices for pickers and filters: grouped under their area, in the user's language.</summary>
public static class TownText
{
    /// <summary>Every town, with its area as <see cref="ChoiceItem.Group"/> (for <c>FilterMenu</c> headings).</summary>
    public static IReadOnlyList<ChoiceItem> TownChoices(this AppText text) =>
        ReferenceData.Areas
            .SelectMany(area => ReferenceData.CitiesByArea[area.Id]
                .Select(town => new ChoiceItem(town.Id, text.Get(town.Label), Group: text.Get(area.Label))))
            .ToList();

    /// <summary>
    /// True when every word typed matches the start of a word in the town's name, ignoring case
    /// and accents ("las pi" → Las Piñas, "nuv" → Santa Rosa (Nuvali)). An empty filter matches all.
    /// </summary>
    public static bool MatchesTown(string? filter, string townLabel)
    {
        var typed = SearchText.Words(filter);
        var words = SearchText.Words(townLabel);
        return typed.All(word => words.Any(known => known.StartsWith(word, StringComparison.Ordinal)));
    }
}
