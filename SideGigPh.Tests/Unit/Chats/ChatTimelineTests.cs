using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Tests.Unit.Chats;

/// <summary>
/// An open chat reads like Messenger: one divider per Philippine day instead of a time under every bubble, and a
/// sender's messages in a row kept together.
/// </summary>
public class ChatTimelineTests
{
    private static readonly TimeSpan Manila = TimeSpan.FromHours(8);

    private static ChatMessage Message(string id, string sender, DateTimeOffset at) =>
        new(id, "ana_me", sender, ChatMessageKind.Text, id, at);

    [Fact]
    public void Group_SplitsByPhilippineDay_EvenWhenUtcSaysOtherwise()
    {
        // 23:30 UTC on the 17th is 07:30 on the 18th in Manila: the same day as the 09:00 message.
        var late = Message("a", "me", new DateTimeOffset(2026, 11, 17, 15, 0, 0, TimeSpan.Zero));
        var morning = Message("b", "ana", new DateTimeOffset(2026, 11, 17, 23, 30, 0, TimeSpan.Zero));
        var later = Message("c", "ana", new DateTimeOffset(2026, 11, 18, 9, 0, 0, Manila));

        var days = ChatTimeline.Group([late, morning, later]);

        days.Select(d => d.Day).Should().Equal(new DateOnly(2026, 11, 17), new DateOnly(2026, 11, 18));
        days[1].Lines.Select(l => l.Message.Id).Should().Equal("b", "c");
    }

    [Fact]
    public void Group_MarksWhereEachSenderStartsTalking_AndEachDayStartsANewRun()
    {
        var day1 = new DateTimeOffset(2026, 11, 18, 9, 0, 0, Manila);
        var messages = new[]
        {
            Message("1", "me", day1),
            Message("2", "me", day1.AddMinutes(1)),
            Message("3", "ana", day1.AddMinutes(2)),
            Message("4", "me", day1.AddMinutes(3)),
            Message("5", "me", day1.AddDays(1)),
        };

        var days = ChatTimeline.Group(messages);

        days[0].Lines.Select(l => l.StartsRun).Should().Equal(true, false, true, true);
        days[1].Lines.Single().StartsRun.Should().BeTrue();
    }

    [Fact]
    public void Group_NoMessages_NoDays() => ChatTimeline.Group([]).Should().BeEmpty();

    [Fact]
    public void DayLabel_TodayYesterdayThenDates()
    {
        var text = new AppText();
        var today = new DateOnly(2026, 11, 20);

        text.ChatDayLabel(today, today).Should().Be("Today");
        text.ChatDayLabel(today.AddDays(-1), today).Should().Be("Yesterday");
        text.ChatDayLabel(new DateOnly(2026, 11, 17), today).Should().Be("Tue, Nov 17");
        text.ChatDayLabel(new DateOnly(2025, 12, 30), today).Should().Be("Dec 30, 2025");
    }
}
