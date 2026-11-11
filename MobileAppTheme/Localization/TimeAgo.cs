using System.Globalization;

namespace MobileAppTheme.Localization;

/// <summary>Short "how long ago" labels for messages, reviews and other timestamps: "Just now", "3 hr ago", "Kahapon", "Nov 3".</summary>
public static class TimeAgo
{
    public static string Ago(this AppText text, DateTimeOffset at)
    {
        var now = DateTimeOffset.Now;
        var elapsed = now - at;
        if (elapsed < TimeSpan.FromMinutes(1))
        {
            return text.Get(CommonCopy.JustNow);
        }

        if (elapsed < TimeSpan.FromHours(1))
        {
            return text.Format(CommonCopy.MinutesAgo, (int)elapsed.TotalMinutes);
        }

        if (elapsed < TimeSpan.FromDays(1))
        {
            return text.Format(CommonCopy.HoursAgo, (int)elapsed.TotalHours);
        }

        var days = (now.Date - at.Date).Days;
        return days switch
        {
            <= 1 => text.Get(CommonCopy.Yesterday),
            < 7 => text.Format(CommonCopy.DaysAgo, days),
            _ => at.ToString("MMM d", CultureInfo.InvariantCulture),
        };
    }
}
