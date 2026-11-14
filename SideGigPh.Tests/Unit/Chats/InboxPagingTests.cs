using SideGigPh.Contracts;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Tests.TestSupport;

namespace SideGigPh.Tests.Unit.Chats;

/// <summary>
/// The Inbox comes <see cref="Paging.Inbox"/> chats at a time (<see cref="InboxPages"/>): a busy worker's Inbox opens
/// fast, older chats load as they scroll, and a refresh never drops what they already scrolled to.
/// </summary>
public class InboxPagingTests
{
    /// <summary><paramref name="count"/> chats with the test Seeker, newest first (pro ids p0, p1, …).</summary>
    private static List<Chat> Chats(int count) =>
        [.. Enumerable.Range(0, count).Select(i =>
        {
            var chat = TestData.ChatWith($"p{i}", $"p{i}");
            return chat with { LastMessage = chat.LastMessage with { At = DateTimeOffset.Now.AddMinutes(-i) } };
        })];

    [Fact]
    public void WithFirstPage_KeepsTheOlderChatsAlreadyLoaded_WithoutRepeats()
    {
        var all = Chats(Paging.Inbox + 5);
        var held = all;
        var fresh = all.Take(Paging.Inbox).ToList();

        InboxPages.WithFirstPage(fresh, pageWasFull: true, held, c => c).Should().Equal(all);
        InboxPages.WithFirstPage(fresh.Take(3).ToList(), pageWasFull: false, held, c => c).Should().HaveCount(3, "a short page is the whole Inbox");
    }

    [Fact]
    public void WithOlder_AddsThePageUnderneath_SkippingChatsAlreadyShown()
    {
        var all = Chats(4);

        InboxPages.WithOlder(all.Take(2).ToList(), all.Skip(1).ToList(), c => c).Should().Equal(all);
    }

    [Fact]
    public async Task Worker_InboxOpensOnTheFirstPage_AndScrollingLoadsTheRest()
    {
        var app = new FakeApp(TestData.FinishedPro(TestData.ReadyDraft()), TestData.Worker());
        app.Chats.Inbox = Chats(Paging.Inbox + 3);
        await app.Pro.EnsureLoadedAsync();
        await app.Pro.RefreshActivityAsync();

        app.Pro.Chats.Should().HaveCount(Paging.Inbox);
        app.Pro.HasMoreChats.Should().BeTrue();

        await app.Pro.MoreChatsAsync();
        await app.Pro.RefreshActivityAsync();

        app.Pro.Chats.Should().Equal(app.Chats.Inbox, "a refresh keeps the older page that was scrolled to");
        app.Pro.HasMoreChats.Should().BeFalse();
        var reads = app.Chats.InboxReads;
        await app.Pro.MoreChatsAsync();
        app.Chats.InboxReads.Should().Be(reads, "nothing more is asked once the end is reached");
    }

    [Fact]
    public async Task Seeker_OlderPage_ContinuesAfterTheLastChat_EvenWhenItsWorkerLeft()
    {
        var chats = Chats(Paging.Inbox + 2);
        var app = await FakeApp.SeekerAsync(before: a =>
        {
            a.Chats.Inbox = chats;
            // The last chat of the first page has no listing any more: its thread is dropped, the paging isn't.
            foreach (var chat in chats.Where(c => c.ProfessionalId != $"p{Paging.Inbox - 1}"))
            {
                a.Profiles.ById[chat.ProfessionalId] = TestData.Listing(chat.ProfessionalId);
            }
        });
        await app.Seeker.RefreshActivityAsync();

        app.Seeker.Threads.Should().HaveCount(Paging.Inbox - 1);
        app.Seeker.HasMoreThreads.Should().BeTrue();

        await app.Seeker.MoreThreadsAsync();

        app.Seeker.Threads.Select(t => t.Pro.Id).Should().Equal(
            chats.Select(c => c.ProfessionalId).Where(id => id != $"p{Paging.Inbox - 1}"));
        app.Seeker.HasMoreThreads.Should().BeFalse();
    }
}
