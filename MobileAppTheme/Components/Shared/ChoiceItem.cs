using MobileAppTheme.Models;
using MobileAppTheme.Localization;

namespace MobileAppTheme.Components.Shared;

/// <summary>
/// One choice (a tile in <see cref="ChoiceTiles"/>, a line in <see cref="FilterMenu"/>), already in the
/// user's language. <see cref="Group"/> is an optional heading for long lists (e.g. a region);
/// items of one group must be next to each other.
/// </summary>
public sealed record ChoiceItem(string Id, string Label, string? Icon = null, string? Hint = null, string? Group = null)
{
    public static IReadOnlyList<ChoiceItem> From(IEnumerable<CatalogOption> options, AppText text) =>
        options
            .Select(o => new ChoiceItem(o.Id, text.Get(o.Label), o.Icon, o.Hint is null ? null : text.Get(o.Hint)))
            .ToList();
}
