using SideGigPh.Contracts;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Tests.TestSupport;

namespace SideGigPh.Tests.Unit.Chats;

/// <summary>
/// A chat opens on its newest <see cref="Paging.Messages"/> messages and loads older ones as the person scrolls to the
/// top. Whatever order pages and new messages arrive in, the chat must read in time order, never show a message twice,
/// never leave a gap, and stop asking once it reaches the first message.
/// </summary>
public class ChatHistoryTests
{
    private const string ChatId = "ana_me";
    private static readonly DateTimeOffset Start = new(2026, 11, 13, 8, 0, 0, TimeSpan.Zero);

    /// <summary>Message n of a chat, one minute apart.</summary>
    private static ChatMessage Message(int n) =>
        new($"m{n:D3}", ChatId, n % 2 == 0 ? "me" : "ana", ChatMessageKind.Text, $"Message {n}", Start.AddMinutes(n));

    private static List<ChatMessage> Messages(int from, int count) => [.. Enumerable.Range(from, count).Select(Message)];

    private static string[] Ids(ChatHistory history) => [.. history.Messages.Select(m => m.Id)];

    [Fact]
    public void FromLatest_FullPage_MayHaveOlder_ShortPageIsTheWholeChat()
    {
        ChatHistory.FromLatest(Messages(30, Paging.Messages)).HasOlder.Should().BeTrue();
        ChatHistory.FromLatest(Messages(0, 3)).HasOlder.Should().BeFalse();
        ChatHistory.FromLatest([]).HasOlder.Should().BeFalse();
    }

    [Fact]
    public void WithOlder_GoesAbove_InTimeOrder_AndStopsAtTheFirstMessage()
    {
        var history = ChatHistory.FromLatest(Messages(25, 20));

        var older = history.WithOlder(Messages(5, 20)).WithOlder(Messages(0, 5));

        Ids(older).Should().Equal(Messages(0, 45).Select(m => m.Id));
        older.HasOlder.Should().BeFalse("the last page was short: that was the first message");
        older.OldestId.Should().Be("m000");
    }

    [Fact]
    public void WithLatest_NewMessage_JoinsAtTheBottom_WithoutRepeatingTheOthers()
    {
        var history = ChatHistory.FromLatest(Messages(0, 5)).WithOlder([]);

        var updated = history.WithLatest(Messages(0, 6));

        Ids(updated).Should().Equal(Messages(0, 6).Select(m => m.Id));
        updated.LastId.Should().Be("m005");
    }

    [Fact]
    public void WithLatest_KeepsOlderPagesAlreadyLoaded()
    {
        var history = ChatHistory.FromLatest(Messages(20, 20)).WithOlder(Messages(0, 20));

        var updated = history.WithLatest(Messages(21, 20));

        updated.Count.Should().Be(41, "the 40 shown plus the one new message");
    }

    [Fact]
    public void WithLatest_ThatDoesNotReachWhatIsShown_ReplacesIt_SoThereIsNoGap()
    {
        // 30 messages came in at once while the phone slept: the newest page starts after the last one shown.
        var history = ChatHistory.FromLatest(Messages(0, 5));

        var updated = history.WithLatest(Messages(35, 20));

        Ids(updated).Should().Equal(Messages(35, 20).Select(m => m.Id));
        updated.HasOlder.Should().BeTrue("the skipped messages load when the person scrolls up");
    }

    [Fact]
    public void With_SavedMessage_ReplacesItsCopy_AndSitsInTimeOrder()
    {
        var sent = Message(3) with { Text = "Sent" };
        var history = ChatHistory.FromLatest(Messages(0, 3)).With(sent).With(sent with { At = sent.At.AddSeconds(1) });

        Ids(history).Should().Equal("m000", "m001", "m002", "m003");
        history.Messages[^1].At.Should().Be(sent.At.AddSeconds(1), "the server's time stamp wins");
        history.Contains("m003").Should().BeTrue();
    }

    [Fact]
    public async Task Workspace_OpensOnTheNewestPage_AndScrollingUpLoadsTheRest()
    {
        var app = await FakeApp.SeekerAsync();
        app.Chats.Messages = [.. Enumerable.Range(0, 45).Select(n => Message(n) with { ChatId = app.Seeker.ChatIdWith("ana") })];

        var opened = await app.Seeker.GetMessagesAsync("ana");
        var all = await app.Seeker.OlderMessagesAsync("ana", await app.Seeker.OlderMessagesAsync("ana", opened));

        opened.Count.Should().Be(Paging.Messages);
        opened.Messages[^1].Text.Should().Be("Message 44");
        all.Count.Should().Be(45);
        all.HasOlder.Should().BeFalse();
        (await app.Seeker.OlderMessagesAsync("ana", all)).Should().BeSameAs(all, "nothing older to ask for");
    }
}
