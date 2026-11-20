using SideGigPh.Contracts;
using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services;

/// <summary>One message in a chat as drawn: <see cref="StartsRun"/> when the one above is the other person's (or none).</summary>
public sealed record ChatLine(ChatMessage Message, bool StartsRun);

/// <summary>The messages of one day (a Philippine date), under its "Today" / "Yesterday" / "Tue, Nov 18" divider.</summary>
public sealed record ChatDay(DateOnly Day, IReadOnlyList<ChatLine> Lines);

/// <summary>
/// How an open chat reads (Messenger style): messages grouped by day under one divider instead of a time under every
/// bubble, and a sender's messages in a row kept close, with more room where the other person starts talking.
/// Dates are the Philippines' dates whatever the phone's time zone, like a post's day.
/// </summary>
public static class ChatTimeline
{
    public static IReadOnlyList<ChatDay> Group(IEnumerable<ChatMessage> messages)
    {
        var days = new List<ChatDay>();
        foreach (var dayMessages in messages.GroupBy(message => DayOf(message.At)))
        {
            var lines = new List<ChatLine>();
            string? lastSender = null;
            foreach (var message in dayMessages)
            {
                lines.Add(new ChatLine(message, message.SenderId != lastSender));
                lastSender = message.SenderId;
            }

            days.Add(new ChatDay(dayMessages.Key, lines));
        }

        return days;
    }

    /// <summary>The Philippine date of a moment.</summary>
    public static DateOnly DayOf(DateTimeOffset at) => DateOnly.FromDateTime(at.ToOffset(JobPostRules.PhilippineOffset).DateTime);
}
