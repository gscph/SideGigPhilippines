using System.Diagnostics.CodeAnalysis;

namespace SideGigPh.Maui.Services;

/// <summary>Validates and normalizes Philippine mobile numbers to E.164 (+639XXXXXXXXX).</summary>
public static class PhilippineMobile
{
    /// <summary>
    /// Accepts 09XXXXXXXXX, 9XXXXXXXXX, 639XXXXXXXXX or +639XXXXXXXXX, with any spaces or dashes.
    /// </summary>
    public static bool TryNormalize(string? input, [NotNullWhen(true)] out string? e164)
    {
        e164 = null;
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        var digits = new string(input.Where(char.IsDigit).ToArray());
        var national = digits switch
        {
            { Length: 11 } when digits.StartsWith("09", StringComparison.Ordinal) => digits[1..],
            { Length: 12 } when digits.StartsWith("639", StringComparison.Ordinal) => digits[2..],
            { Length: 10 } when digits.StartsWith('9') => digits,
            _ => null,
        };

        if (national is null)
        {
            return false;
        }

        e164 = "+63" + national;
        return true;
    }

    /// <summary>+639171234567 → 09171234567, for showing a saved number back in an input.</summary>
    public static string ToLocal(string? e164) =>
        e164 is { Length: 13 } && e164.StartsWith("+63", StringComparison.Ordinal) ? "0" + e164[3..] : e164 ?? string.Empty;
}
