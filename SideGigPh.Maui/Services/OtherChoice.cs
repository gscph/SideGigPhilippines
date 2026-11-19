using MobileAppTheme.Models;
using SideGigPh.Contracts;

namespace SideGigPh.Maui.Services;

/// <summary>
/// A one-pick list (trades, a Seeker's jobs) once "Others" is picked: the list folds to that one tile, so the box
/// for what they type sits right under the question and stays above the phone's keyboard. "Show the list" opens it
/// again; picking "Others" folds it again.
/// </summary>
public static class OtherChoice
{
    /// <summary>Only the picked "Others" tile while folded; otherwise every option.</summary>
    public static IReadOnlyList<CatalogOption> Shown(IReadOnlyList<CatalogOption> options, string? pickedId, bool showAll) =>
        !showAll && OtherWork.IsOther(pickedId) && options.FirstOrDefault(o => o.Id == pickedId) is { } other
            ? [other]
            : options;

    /// <summary>True when <see cref="Shown"/> hid the rest, so "Show the list" is offered.</summary>
    public static bool IsFolded(IReadOnlyList<CatalogOption> options, string? pickedId, bool showAll) =>
        Shown(options, pickedId, showAll).Count < options.Count;
}
