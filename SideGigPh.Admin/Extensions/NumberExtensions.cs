using System.Globalization;

namespace SideGigPh.Admin.Extensions;

public static class NumberExtensions
{
    private static readonly CultureInfo Philippines = CultureInfo.GetCultureInfo("en-PH");

    /// <summary><c>₱1,500</c>; a dash for 0 (no price yet).</summary>
    public static string Pesos(this int value) => value <= 0 ? "—" : "₱" + value.ToString("N0", Philippines);

    /// <summary><c>1,234</c>.</summary>
    public static string Count(this int value) => value.ToString("N0", Philippines);

    /// <summary><c>25%</c> of <paramref name="total"/>; a dash when the total is 0.</summary>
    public static string PercentOf(this int value, int total) =>
        total <= 0 ? "—" : (value / (double)total).ToString("P0", Philippines);
}
