using System.Globalization;

namespace SideGigPh.Maui.Services;

/// <summary>Peso formatting and the earnings-goal arithmetic behind the Professional payoff.</summary>
public static class Money
{
    public static string Peso(int amount) => "₱" + amount.ToString("N0", CultureInfo.InvariantCulture);
}
