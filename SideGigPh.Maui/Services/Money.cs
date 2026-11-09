using System.Globalization;

namespace SideGigPh.Maui.Services;

/// <summary>Peso formatting and the earnings-goal arithmetic behind the Professional payoff.</summary>
public static class Money
{
    private const double WeeksPerMonth = 52.0 / 12.0;

    public static string Peso(int amount) => "₱" + amount.ToString("N0", CultureInfo.InvariantCulture);

    /// <summary>Jobs a week needed to reach <paramref name="monthlyGoal"/> at <paramref name="pricePerJob"/>; at least 1.</summary>
    public static int JobsPerWeek(int monthlyGoal, int pricePerJob) =>
        Math.Max(1, (int)Math.Ceiling(monthlyGoal / (pricePerJob * WeeksPerMonth)));
}
