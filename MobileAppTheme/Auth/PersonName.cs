namespace MobileAppTheme.Auth;

/// <summary>Short forms of a person's full name.</summary>
public static class PersonName
{
    /// <summary>
    /// First name and the initial of the last word, e.g. "Maricel Dela Cruz" → "Maricel C.",
    /// "Maricel" → "Maricel". For places where a full name is more than strangers need to see.
    /// </summary>
    public static string? Short(string? fullName)
    {
        var parts = fullName?.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts switch
        {
            null or [] => null,
            [var only] => only,
            [var first, .., var last] => $"{first} {char.ToUpperInvariant(last[0])}.",
        };
    }
}
