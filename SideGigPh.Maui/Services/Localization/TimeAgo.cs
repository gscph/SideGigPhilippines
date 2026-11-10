using System.Globalization;

namespace SideGigPh.Maui.Services.Localization;

/// <summary>Short "how long ago" labels for inquiries and reviews: "Just now", "3 hr ago", "Kahapon", "Nov 3".</summary>
public static class TimeAgo
{
    public static string Ago(this AppText text, DateTimeOffset at)
    {
        var now = DateTimeOffset.Now;
        var elapsed = now - at;
        if (elapsed < TimeSpan.FromMinutes(1))
        {
            return text.Get(Copy.JustNow);
        }

        if (elapsed < TimeSpan.FromHours(1))
        {
            return text.Format(Copy.MinutesAgo, (int)elapsed.TotalMinutes);
        }

        if (elapsed < TimeSpan.FromDays(1))
        {
            return text.Format(Copy.HoursAgo, (int)elapsed.TotalHours);
        }

        var days = (now.Date - at.Date).Days;
        return days switch
        {
            <= 1 => text.Get(Copy.Yesterday),
            < 7 => text.Format(Copy.DaysAgo, days),
            _ => at.ToString("MMM d", CultureInfo.InvariantCulture),
        };
    }
}
