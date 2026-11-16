using Microsoft.Extensions.Logging.Abstractions;
using SideGigPh.Contracts;
using SideGigPh.Maui.Services;
using SideGigPh.Tests.TestSupport;

namespace SideGigPh.Tests.Unit.Chats;

/// <summary>
/// The phone side of new-message notifications: the permission is asked only when there's a reason, the phone is
/// registered once allowed, a tapped notification opens its chat (even the tap that started the app), a message that
/// arrives while the app is open refreshes the Inbox, and the icon badge follows what's unread.
/// </summary>
public class ChatAlertsTests
{
    private readonly FakePush _push = new();
    private readonly FakeUsers _users = new();

    private ChatAlerts Alerts() => new(_push, _users, NullLogger<ChatAlerts>.Instance);

    private static Dictionary<string, string> ToWorker(string chatId) => new()
    {
        [PushData.Side] = Wire.Professional, [PushData.ChatId] = chatId, [PushData.ProfessionalId] = "pro-1",
    };

    [Fact]
    public async Task Register_NotAllowed_NeverAsks_AndSendsNothing()
    {
        await Alerts().RegisterAsync();

        _push.Asked.Should().Be(0, "only Ask shows the system dialog");
        _users.Devices.Should().BeEmpty();
    }

    [Fact]
    public async Task Register_Allowed_SendsTheTokenAndPlatform()
    {
        _push.Allowed = true;

        await Alerts().RegisterAsync();

        _users.Devices.Should().Equal(("token-1", "android"));
    }

    [Fact]
    public async Task Ask_PersonAllows_RegistersThePhone()
    {
        await Alerts().AskAsync();

        _push.Asked.Should().Be(1);
        _users.Devices.Should().ContainSingle();
    }

    [Fact]
    public async Task Ask_PersonSaysNo_RegistersNothing()
    {
        _push.AllowsWhenAsked = false;

        await Alerts().AskAsync();

        _users.Devices.Should().BeEmpty();
    }

    [Fact]
    public async Task Ask_AlreadyAllowed_DoesNotShowTheDialogAgain()
    {
        _push.Allowed = true;

        await Alerts().AskAsync();

        _push.Asked.Should().Be(0);
        _users.Devices.Should().ContainSingle();
    }

    [Fact]
    public async Task NoPushProvider_DoesNothing()
    {
        _push.Platform = null;

        await Alerts().AskAsync();

        _push.Asked.Should().Be(0);
        _users.Devices.Should().BeEmpty();
    }

    [Fact]
    public async Task Register_ServerDown_IsLoggedNotThrown()
    {
        _push.Allowed = true;
        _users.FailSaves = true;

        var register = () => Alerts().RegisterAsync();

        await register.Should().NotThrowAsync("a missing notification must never break the screen");
    }

    [Fact]
    public void TokenChanges_TheNewOneIsSent()
    {
        _ = Alerts();

        _push.ChangeToken("token-2");

        _users.Devices.Should().Equal(("token-2", "android"));
    }

    [Fact]
    public void Tap_WhileTheAppIsOpen_OpensTheChat()
    {
        var alerts = Alerts();
        var opened = new List<string>();
        using var _ = alerts.Attach(() => Task.CompletedTask, opened.Add);

        _push.Tap(ToWorker("pro-1_seeker-1"));

        opened.Should().Equal("/professional/inbox/pro-1_seeker-1");
    }

    [Fact]
    public void Tap_ThatStartedTheApp_OpensTheChatWhenTheLayoutAttaches()
    {
        var alerts = Alerts();
        _push.Tap(ToWorker("pro-1_seeker-1"));
        var opened = new List<string>();

        using var _ = alerts.Attach(() => Task.CompletedTask, opened.Add);

        opened.Should().Equal("/professional/inbox/pro-1_seeker-1");
    }

    [Fact]
    public void Tap_AfterTheLayoutWent_WaitsForTheNextOne()
    {
        var alerts = Alerts();
        var first = new List<string>();
        alerts.Attach(() => Task.CompletedTask, first.Add).Dispose();

        _push.Tap(ToWorker("c1"));
        var second = new List<string>();
        using var _ = alerts.Attach(() => Task.CompletedTask, second.Add);

        first.Should().BeEmpty();
        second.Should().Equal("/professional/inbox/c1");
    }

    [Fact]
    public void Arrival_WhileOpen_RefreshesTheInbox()
    {
        var alerts = Alerts();
        var refreshes = 0;
        using var _ = alerts.Attach(() => { refreshes++; return Task.CompletedTask; }, _ => { });

        _push.Arrive(ToWorker("c1"));

        refreshes.Should().Be(1);
    }

    [Fact]
    public void Arrival_RefreshFails_IsLoggedNotThrown()
    {
        var alerts = Alerts();
        using var _ = alerts.Attach(() => throw new HttpRequestException("offline"), _ => { });

        var arrive = () => _push.Arrive(ToWorker("c1"));

        arrive.Should().NotThrow();
    }

    [Fact]
    public void ShowUnread_HandsTheUnreadChatsToTheBadge()
    {
        Alerts().ShowUnread(["c1", "c2"]);

        _push.Shown.Should().Equal("c1", "c2");
    }

    [Theory]
    [InlineData(Wire.Seeker, "c1", "pro 1", "/inbox/pro%201")]
    [InlineData(Wire.Professional, "a_b", "pro-1", "/professional/inbox/a_b")]
    [InlineData("someone", "c1", "pro-1", null)]
    [InlineData(Wire.Seeker, "c1", "", null)]
    [InlineData(Wire.Professional, "", "pro-1", null)]
    public void PathFor_OpensTheChatOnTheReceiversSide(string side, string chatId, string proId, string? expected) =>
        ChatAlerts.PathFor(new Dictionary<string, string>
        {
            [PushData.Side] = side, [PushData.ChatId] = chatId, [PushData.ProfessionalId] = proId,
        }).Should().Be(expected);

    [Theory]
    [InlineData(Wire.Professional, "42", "/professional/requests")]
    [InlineData(Wire.Seeker, "42", "/posts/42/offers")]
    [InlineData(Wire.Seeker, "not-a-number", null)]
    [InlineData("someone", "42", null)]
    public void PathFor_AJobPost_OpensTheFeedOrTheOffers(string side, string postId, string? expected) =>
        ChatAlerts.PathFor(new Dictionary<string, string> { [PushData.Side] = side, [PushData.PostId] = postId })
            .Should().Be(expected);

    [Fact]
    public async Task Workspaces_ListTheUnreadChats_ForTheBadge()
    {
        var app = await FakeApp.ProAsync(TestData.ReadyDraft());
        var unread = TestData.ChatWith(TestData.WorkerUid, TestData.SeekerUid);
        var answered = TestData.ChatWith("other-pro", TestData.WorkerUid) with { Id = "answered", ProfessionalId = TestData.WorkerUid };
        app.Chats.Inbox = [unread, answered];

        await app.Pro.RefreshActivityAsync();

        app.Pro.UnreadChatIds.Should().Equal(unread.Id);
        app.Pro.UnreadCount.Should().Be(1);
    }
}
