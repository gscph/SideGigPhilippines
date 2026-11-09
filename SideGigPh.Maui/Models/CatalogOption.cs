namespace SideGigPh.Maui.Models;

/// <summary>
/// One answer in a reference list (category, job, town, price tier, …).
/// <see cref="Amount"/> is the peso value for money options and 0 for everything else.
/// </summary>
public sealed record CatalogOption(
    string Id,
    LocalizedText Label,
    string? Icon = null,
    LocalizedText? Hint = null,
    int Amount = 0);
