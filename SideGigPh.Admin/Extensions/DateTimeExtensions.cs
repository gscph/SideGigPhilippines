namespace SideGigPh.Admin.Extensions;

/// <summary>Times on the site are Philippine time (UTC+8), like the dashboard's days, whatever the browser's zone.</summary>
public static class DateTimeExtensions
{
    public static readonly TimeSpan Manila = TimeSpan.FromHours(8);

    /// <summary><c>20 Nov 2026, 3:05 PM</c> in Philippine time; a dash when there is none.</summary>
    public static string ToManila(this DateTimeOffset? value) => value is { } at ? at.ToManila() : "—";

    public static string ToManila(this DateTimeOffset value) => value.ToOffset(Manila).ToString("d MMM yyyy, h:mm tt");

    /// <summary><c>20 Nov 2026</c> in Philippine time.</summary>
    public static string ToManilaDate(this DateTimeOffset value) => value.ToOffset(Manila).ToString("d MMM yyyy");

    /// <summary>"just now", "5 min ago", "3 hr ago", "2 days ago", else the date; "never" when there is none.</summary>
    public static string Ago(this DateTimeOffset? value, DateTimeOffset now)
    {
        if (value is not { } at)
        {
            return "never";
        }

        var gone = now - at;
        return gone.TotalMinutes < 1 ? "just now"
            : gone.TotalHours < 1 ? $"{(int)gone.TotalMinutes} min ago"
            : gone.TotalDays < 1 ? $"{(int)gone.TotalHours} hr ago"
            : gone.TotalDays < 30 ? $"{(int)gone.TotalDays} day{((int)gone.TotalDays == 1 ? "" : "s")} ago"
            : at.ToManilaDate();
    }
}
