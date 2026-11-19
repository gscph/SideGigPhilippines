using SideGigPh.Contracts;

namespace SideGigPh.Maui.Services;

/// <summary>
/// The day a household needs the work done (a post's "When", picked on a calendar; optional: no day = any day). The
/// dates are the Philippines' dates whatever the phone's time zone, from today to <see cref="JobPostRules.MaxDaysAhead"/>
/// days ahead. Search never uses it (search keeps the interview's "When" answer only to word the first message).
/// </summary>
public static class PostDay
{
    /// <summary>Today's date in the Philippines.</summary>
    public static DateOnly Today(TimeProvider clock) => JobPostRules.PhilippineToday(clock.GetUtcNow());

    /// <summary>The last day the calendar offers.</summary>
    public static DateOnly LastDay(DateOnly today) => today.AddDays(JobPostRules.MaxDaysAhead);

    /// <summary>
    /// True when <paramref name="day"/> can be sent: none, a day from today, or the day the post already had (Edit post:
    /// a day that has passed may stay, as the server allows).
    /// </summary>
    public static bool IsValid(DateOnly? day, DateOnly? saved, DateOnly today) =>
        day == saved || JobPostRules.IsNeededOn(day, today);
}
