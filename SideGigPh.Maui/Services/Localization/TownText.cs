namespace SideGigPh.Maui.Services.Localization;

/// <summary>The towns and barangays we serve as choices for pickers and filters, and a place as words, in the user's language.</summary>
public static class TownText
{
    /// <summary>Every town (one area, so no <see cref="ChoiceItem.Group"/> headings) for <c>FilterMenu</c>.</summary>
    public static IReadOnlyList<ChoiceItem> TownChoices(this AppText text) => ChoiceItem.From(ReferenceData.Cities, text);

    /// <summary>
    /// A town, and its barangay when one we serve was picked: "Angono · Brgy. San Isidro", else "Angono". A town we no
    /// longer serve shows its id.
    /// </summary>
    public static string Place(this AppText text, string cityId, string? barangayId)
    {
        var town = ReferenceData.Find(ReferenceData.Cities, cityId) is { } found ? text.Get(found.Label) : cityId;
        return ReferenceData.FindBarangay(cityId, barangayId) is { } barangay
            ? text.Format(Copy.PlaceBarangay, town, text.Get(barangay.Label))
            : town;
    }
}
