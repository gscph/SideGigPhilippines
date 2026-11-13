using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Tests.TestSupport;

namespace SideGigPh.Tests.Unit.Chats;

/// <summary>
/// Contacting a worker is how a Seeker gets a job done, and how a worker gets paid. These follow both Inboxes:
/// the ready-made first message, Send and Call, the unread badge, replies and live updates. A broken step here means
/// a lost customer.
/// </summary>
public class InboxTests
{
    private static ProfessionalListing Ana() => TestData.Listing("Ana", services: ["aircon-cleaning", "aircon-leak"]);

    // --- Seeker side ----------------------------------------------------------

    [Theory]
    [InlineData(ReferenceData.UrgencyToday, "Hi! I found you on SideGigPh. I need: Aircon cleaning, in Taytay, today. Are you available?")]
    [InlineData(ReferenceData.UrgencyThisWeek, "Hi! I found you on SideGigPh. I need: Aircon cleaning, in Taytay, this week. Are you available?")]
    [InlineData(ReferenceData.UrgencyCanvassing, "Hi! I found you on SideGigPh. I need: Aircon cleaning, in Taytay. How much do you charge?")]
    public async Task DefaultMessage_SaysTheJobTownAndHowSoon(string urgency, string expected)
    {
        var answers = TestData.AirconCleaningInTaytay();
        answers.UrgencyId = urgency;
        var app = await FakeApp.SeekerAsync(answers);

        app.Seeker.DefaultMessage(Ana()).Should().Be(expected);
    }

    [Fact]
    public async Task DefaultMessage_ProDoesNotListTheJob_NamesTheTradeInstead()
    {
        var app = await FakeApp.SeekerAsync();
        var installer = TestData.Listing("Ike", services: ["aircon-install"]);

        app.Seeker.DefaultMessage(installer).Should().Contain("I need: Aircon,");
    }

    [Fact]
    public async Task DefaultMessage_NoTownAnswer_UsesTheWorkersTown_InTaglish()
    {
        var answers = TestData.AirconCleaningInTaytay();
        answers.CityId = null;
        var app = await FakeApp.SeekerAsync(answers);
        app.Text.Use(AppLanguage.Taglish);

        app.Seeker.DefaultMessage(TestData.Listing("Ana", cities: ["angono"]))
            .Should().Be("Hi po! Nakita ko kayo sa SideGigPh. Kailangan ko po: Pa-linis ng aircon, dito sa Angono, ngayon. Available po ba kayo?");
    }

    [Fact]
    public async Task DefaultMessage_UnknownTradeAndTown_LeavesThemBlankInsteadOfCrashing()
    {
        var answers = new SeekerAnswers();
        var app = await FakeApp.SeekerAsync(answers);
        var odd = new ProfessionalListing { Id = "x", DisplayName = "X", CategoryIds = [], ServiceIds = [], CityIds = [], MobileNumber = "" };

        app.Seeker.DefaultMessage(odd).Should().Contain("I need: , in .");
    }

    [Fact]
    public async Task Send_FirstMessage_StartsTheChatWithTheJobFromTheAnswers()
    {
        var app = await FakeApp.SeekerAsync();

        var sent = await app.Seeker.SendAsync(Ana(), "  Hello po  ");

        var (chat, message) = app.Chats.Sent.Single();
        chat.Id.Should().Be(Chat.IdFor("ana", TestData.SeekerUid));
        chat.ServiceId.Should().Be("aircon-cleaning");
        chat.CityId.Should().Be("taytay");
        chat.UrgencyId.Should().Be(ReferenceData.UrgencyToday);
        chat.BudgetId.Should().Be("500-1000");
        chat.SeekerName.Should().Be("Maricel C.");
        message.Text.Should().Be("Hello po");
        message.Kind.Should().Be(ChatMessageKind.Text);
        sent.Should().Be(message with { At = message.At.AddSeconds(1) }, "the page shows the message as the server saved it");
        app.Seeker.ThreadWith("ana").Should().NotBeNull("the Inbox shows the new chat at once");
    }

    [Fact]
    public async Task Send_NoNameYet_UsesCustomer()
    {
        var app = await FakeApp.SeekerAsync(user: new AuthUser { Uid = TestData.SeekerUid });

        await app.Seeker.SendAsync(Ana(), "Hi");

        app.Chats.Sent.Single().Chat.SeekerName.Should().Be("Customer");
    }

    [Fact]
    public async Task Call_DialerOpens_LogsTheCallInTheChat()
    {
        var app = await FakeApp.SeekerAsync();

        var logged = await app.Seeker.CallAsync(Ana());

        app.Phone.Called.Should().Equal(TestData.WorkerNumber);
        logged!.Kind.Should().Be(ChatMessageKind.Call);
        logged.Text.Should().BeNull();
    }

    [Fact]
    public async Task Call_DialerDoesNotOpen_LogsNothing()
    {
        var app = await FakeApp.SeekerAsync();
        app.Phone.Opens = false;

        (await app.Seeker.CallAsync(Ana())).Should().BeNull();
        app.Chats.Sent.Should().BeEmpty();
    }

    [Fact]
    public async Task Inbox_ShowsChatsWithTheirWorkers_AndDropsWorkersWhoLeft()
    {
        var app = await FakeApp.SeekerAsync(before: a =>
        {
            a.Chats.Inbox = [TestData.ChatWith("ana", "ana"), TestData.ChatWith("gone", "gone")];
            a.Profiles.ById["ana"] = Ana();
        });

        app.Seeker.Threads.Should().ContainSingle().Which.Pro.Id.Should().Be("ana");
        app.Seeker.UnreadCount.Should().Be(1, "Ana wrote last and the Seeker hasn't opened it");
        app.Seeker.ToRate.Should().ContainSingle("contacted workers can be rated");
    }

    [Fact]
    public async Task MarkRead_UnreadChat_ClearsTheBadge()
    {
        var app = await FakeApp.SeekerAsync(before: a =>
        {
            a.Chats.Inbox = [TestData.ChatWith("ana", "ana")];
            a.Profiles.ById["ana"] = Ana();
        });

        await app.Seeker.MarkReadAsync("ana");

        app.Chats.MarkedRead.Should().Equal((Chat.IdFor("ana", TestData.SeekerUid), ChatSide.Seeker));
        app.Seeker.UnreadCount.Should().Be(0);
    }

    [Fact]
    public async Task MarkRead_AlreadyRead_DoesNotCallTheServer()
    {
        var app = await FakeApp.SeekerAsync(before: a =>
        {
            a.Chats.Inbox = [TestData.ChatWith("ana", TestData.SeekerUid)];
            a.Profiles.ById["ana"] = Ana();
        });

        await app.Seeker.MarkReadAsync("ana");
        await app.Seeker.MarkReadAsync("nobody");

        app.Chats.MarkedRead.Should().BeEmpty();
    }

    [Fact]
    public async Task ChatChanged_NewMessageInAWatchedChat_MovesItToTheTop()
    {
        var app = await FakeApp.SeekerAsync(before: a =>
        {
            a.Chats.Inbox = [TestData.ChatWith("ben", "ben"), TestData.ChatWith("ana", "ana")];
            a.Profiles.ById["ana"] = Ana();
            a.Profiles.ById["ben"] = TestData.Listing("Ben");
        });
        var changed = 0;
        app.Seeker.Changed += () => changed++;

        app.Seeker.ChatChanged(TestData.ChatWith("ana", "ana"));
        app.Seeker.ChatChanged(TestData.ChatWith("stranger", "stranger")); // not in the Inbox: ignored

        app.Seeker.Threads.Select(t => t.Pro.Id).Should().Equal("ana", "ben");
        changed.Should().Be(1);
    }

    [Fact]
    public async Task MessagesAndWatch_UseTheChatWithThatWorker()
    {
        var app = await FakeApp.SeekerAsync();
        var chatId = Chat.IdFor("ana", TestData.SeekerUid);
        app.Chats.Messages = [new ChatMessage("m1", chatId, "ana", ChatMessageKind.Text, "Hi", DateTimeOffset.Now)];

        (await app.Seeker.GetMessagesAsync("ana")).Should().ContainSingle();
        using (app.Seeker.WatchChat("ana", "m1", _ => { }))
        {
            app.Chats.Watching.Should().Be((chatId, "m1"));
        }

        app.Seeker.ChatIdWith("ana").Should().Be(chatId);
    }

    // --- Worker side ----------------------------------------------------------

    private static async Task<FakeApp> WorkerWithInbox(params Chat[] chats)
    {
        var app = new FakeApp(TestData.FinishedPro(TestData.ReadyDraft()), TestData.Worker());
        app.Chats.Inbox = [.. chats];
        await app.Pro.EnsureLoadedAsync();
        return app;
    }

    private static Chat CustomerChat(string seekerId, string lastSender, DateTimeOffset? proReadAt = null)
    {
        var id = Chat.IdFor(TestData.WorkerUid, seekerId);
        return new Chat(id, TestData.WorkerUid, seekerId, "Juan", seekerId, null, null, null, null,
            new ChatMessage("m-" + seekerId, id, lastSender, ChatMessageKind.Text, "Hi", DateTimeOffset.Now), null, proReadAt, DateTimeOffset.Now);
    }

    [Fact]
    public async Task WorkerInbox_CountsChatsWhereTheCustomerWroteLast()
    {
        var app = await WorkerWithInbox(CustomerChat("s1", "s1"), CustomerChat("s2", TestData.WorkerUid));

        app.Pro.Chats.Should().HaveCount(2);
        app.Pro.UnreadCount.Should().Be(1);
    }

    [Fact]
    public async Task WorkerReply_IsSentAsTheWorker_AndShownAtTheTop()
    {
        var chat = CustomerChat("s1", "s1");
        var app = await WorkerWithInbox(CustomerChat("s2", "s2"), chat);

        var reply = await app.Pro.ReplyAsync(chat, "  Available po  ");

        reply.SenderId.Should().Be(TestData.WorkerUid);
        reply.Text.Should().Be("Available po");
        app.Pro.Chats[0].Id.Should().Be(chat.Id);
        app.Pro.UnreadCount.Should().Be(1, "the worker's own reply makes that chat read");
    }

    [Fact]
    public async Task WorkerMarkRead_OnlyCallsTheServerForUnreadChats()
    {
        var unread = CustomerChat("s1", "s1");
        var app = await WorkerWithInbox(unread, CustomerChat("s2", TestData.WorkerUid));

        await app.Pro.MarkReadAsync(unread.Id);
        await app.Pro.MarkReadAsync(Chat.IdFor(TestData.WorkerUid, "s2"));

        app.Chats.MarkedRead.Should().Equal((unread.Id, ChatSide.Professional));
        app.Pro.UnreadCount.Should().Be(0);
    }

    [Fact]
    public async Task WorkerChatChanged_PutsTheChatOnTop()
    {
        var app = await WorkerWithInbox(CustomerChat("s1", "s1"), CustomerChat("s2", "s2"));

        app.Pro.ChatChanged(CustomerChat("s2", "s2"));

        app.Pro.Chats[0].SeekerId.Should().Be("s2");
    }

    [Fact]
    public async Task WorkerChatReads_GoToTheChatStore()
    {
        var chat = CustomerChat("s1", "s1");
        var app = await WorkerWithInbox(chat);
        app.Chats.Messages = [chat.LastMessage];

        (await app.Pro.GetChatAsync(chat.Id)).Should().Be(chat);
        (await app.Pro.GetMessagesAsync(chat.Id)).Should().ContainSingle();
        using (app.Pro.WatchChat(chat.Id, "m-s1", _ => { }))
        {
            app.Chats.Watching.Should().Be((chat.Id, "m-s1"));
        }
    }
}
