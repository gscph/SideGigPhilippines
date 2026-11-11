using MobileAppTheme.Models;
using MobileAppTheme.Localization;

namespace MobileAppTheme.Components.Shared;

/// <summary>One tile in <see cref="ChoiceTiles"/>, already in the user's language.</summary>
public sealed record ChoiceItem(string Id, string Label, string? Icon = null, string? Hint = null)
{
    public static IReadOnlyList<ChoiceItem> From(IEnumerable<CatalogOption> options, AppText text) =>
        options
            .Select(o => new ChoiceItem(o.Id, text.Get(o.Label), o.Icon, o.Hint is null ? null : text.Get(o.Hint)))
            .ToList();
}
