namespace SideGigPh.Maui.Services.Localization;

/// <summary>The towns we serve as choices for pickers and filters, in the user's language.</summary>
public static class TownText
{
    /// <summary>Every town (one area, so no <see cref="ChoiceItem.Group"/> headings) for <c>FilterMenu</c>.</summary>
    public static IReadOnlyList<ChoiceItem> TownChoices(this AppText text) => ChoiceItem.From(ReferenceData.Cities, text);
}
