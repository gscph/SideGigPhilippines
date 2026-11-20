using SideGigPh.Contracts;
using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services;

/// <summary>
/// One message in a chat as drawn: <see cref="StartsRun"/> when the one above is the other person's (or none),
/// <see cref="EndsRun"/> when the one below is (or none), where its "3 hr ago" shows.
/// </summary>
public sealed record ChatLine(ChatMessage Message, bool StartsRun, bool EndsRun);

/// <summary>The messages of one day (a Philippine date), under its "Today" / "Yesterday" / "Tue, Nov 18" divider.</summary>
public sealed record ChatDay(DateOnly Day, IReadOnlyList<ChatLine> Lines);

/// <summary>
/// How an open chat reads (Messenger style): messages grouped by day under one divider instead of a time under every
/// bubble, a sender's messages in a row kept close, with more room where the other person starts talking, and the
/// "Just now" / "3 hr ago" once under the last message of each run.
/// Dates are the Philippines' dates whatever the phone's time zone, like a post's day.
/// </summary>
public static class ChatTimeline
{
    public static IReadOnlyList<ChatDay> Group(IEnumerable<ChatMessage> messages)
    {
        var days = new List<ChatDay>();
        foreach (var dayMessages in messages.GroupBy(message => DayOf(message.At)))
        {
            var messagesOfDay = dayMessages.ToList();
            var lines = new List<ChatLine>(messagesOfDay.Count);
            for (var i = 0; i < messagesOfDay.Count; i++)
            {
                var sender = messagesOfDay[i].SenderId;
                var startsRun = i == 0 || messagesOfDay[i - 1].SenderId != sender;
                var endsRun = i == messagesOfDay.Count - 1 || messagesOfDay[i + 1].SenderId != sender;
                lines.Add(new ChatLine(messagesOfDay[i], startsRun, endsRun));
            }

            days.Add(new ChatDay(dayMessages.Key, lines));
        }

        return days;
    }

    /// <summary>The Philippine date of a moment.</summary>
    public static DateOnly DayOf(DateTimeOffset at) => JobPostRules.PhilippineToday(at);
}
