using MobileAppTheme.Localization;

namespace MobileAppTheme.Models;

/// <summary>
/// One answer in a reference list (category, town, price tier, …); <c>ChoiceItem.From</c> turns a list into tiles.
/// <see cref="Amount"/> is the money value (whole units) for price options and 0 for everything else.
/// </summary>
public sealed record CatalogOption(
    string Id,
    LocalizedText Label,
    string? Icon = null,
    LocalizedText? Hint = null,
    int Amount = 0);
