using FirebaseAdmin.Messaging;
using Microsoft.Extensions.Logging.Abstractions;
using SideGigPh.Api;
using SideGigPh.AzureSQL;
using SideGigPh.Contracts;

namespace SideGigPh.Tests.Unit.Chats;

/// <summary>
/// What the server's notification says and how loud it is. A customer writing to a Skilled Worker rings; a worker
/// answering a Seeker only sets the badge, and on iOS must carry no alert at all. Wording follows the receiver's language.
/// </summary>
public class ChatNotifierTests
{
    private static readonly DateTimeOffset Start = new(2026, 11, 1, 8, 0, 0, TimeSpan.Zero);

    private static ChatDto Chat(string senderId, string kind = Wire.Text, string? text = "Hello po", bool first = true) => new(
        "pro-1_seeker-1", "pro-1", "seeker-1", "Rico", "Liza", "aircon-cleaning", "taytay", null, null,
        new MessageDto("m1", "pro-1_seeker-1", senderId, kind, text, first ? Start : Start.AddMinutes(5)),
        null, null, Start);

    private static PushTarget Target(string language = Wire.English, int unread = 2) => new(["t1", "t2"], language, unread);

    [Fact]
    public void ToWorker_FirstMessage_IsALoudNewCustomerNotification()
    {
        var message = ChatNotifier.Message(Chat("seeker-1"), toWorker: true, Target());

        message.Alert.Should().BeTrue();
        message.Title.Should().Be("New customer: Liza");
        message.Body.Should().Be("Hello po");
        message.AndroidChannel.Should().Be(PushData.CustomersChannel);
        message.Tokens.Should().Equal("t1", "t2");
        message.Badge.Should().Be(2);
        message.Tag.Should().Be("pro-1_seeker-1");
        message.Data.Should().Contain(PushData.Side, Wire.Professional).And.Contain("is_silent_in_foreground", "true");
    }

    [Fact]
    public void ToWorker_LaterMessage_InTaglish_IsJustTheName()
    {
        var message = ChatNotifier.Message(Chat("seeker-1", first: false), toWorker: true, Target(Wire.Taglish));

        message.Title.Should().Be("Liza");
    }

    [Theory]
    [InlineData(Wire.English, "Called you")]
    [InlineData(Wire.Taglish, "Tumawag sa'yo")]
    public void ToWorker_Call_SaysTheyWereCalled(string language, string body) =>
        ChatNotifier.Message(Chat("seeker-1", Wire.Call, text: null), toWorker: true, Target(language)).Body.Should().Be(body);

    [Fact]
    public void ToSeeker_IsQuiet_OnTheRepliesChannel()
    {
        var message = ChatNotifier.Message(Chat("pro-1"), toWorker: false, Target());

        message.Alert.Should().BeFalse();
        message.Title.Should().Be("Rico");
        message.AndroidChannel.Should().Be(PushData.RepliesChannel);
        message.Data[PushData.Side].Should().Be(Wire.Seeker);
    }

    [Fact]
    public void LongMessage_IsCut_OnOneLine()
    {
        var body = ChatNotifier.Message(Chat("seeker-1", text: "Line one\nline two " + new string('x', 200)), toWorker: true, Target()).Body;

        body.Should().HaveLength(ChatNotifier.MaxPreview).And.EndWith("…").And.StartWith("Line one line two");
    }

    [Fact]
    public void Fcm_Quiet_HasNoIosAlertOrSound_OnlyTheBadge()
    {
        var fcm = FcmPushSender.ToFcm(ChatNotifier.Message(Chat("pro-1"), toWorker: false, Target()));

        fcm.Notification.Should().BeNull("a top-level notification always shows an alert on iOS");
        fcm.Apns.Aps.Alert.Should().BeNull();
        fcm.Apns.Aps.Sound.Should().BeNull();
        fcm.Apns.Aps.Badge.Should().Be(2);
        fcm.Apns.Headers["apns-collapse-id"].Should().Be("pro-1_seeker-1", "the app clears a read chat's notification by this id");
        fcm.Android.Priority.Should().Be(Priority.Normal);
        fcm.Android.Notification.ChannelId.Should().Be(PushData.RepliesChannel);
        fcm.Android.Notification.Tag.Should().Be("pro-1_seeker-1", "the app clears a read chat's notification by this tag");
    }

    [Fact]
    public void Fcm_Loud_RingsOnBothPlatforms()
    {
        var fcm = FcmPushSender.ToFcm(ChatNotifier.Message(Chat("seeker-1"), toWorker: true, Target()));

        fcm.Apns.Aps.Alert.Title.Should().Be("New customer: Liza");
        fcm.Apns.Aps.Sound.Should().Be("default");
        fcm.Android.Priority.Should().Be(Priority.High);
        fcm.Android.Notification.Title.Should().Be("New customer: Liza");
        fcm.Data[PushData.ChatId].Should().Be("pro-1_seeker-1");
#pragma warning disable CS0618
        fcm.Tokens.Should().Equal("t1", "t2");
#pragma warning restore CS0618
    }

    [Fact]
    public async Task NoPushSender_SendsNothing_AndForgetsNoToken()
    {
        var dead = await new NoPushSender(NullLogger<NoPushSender>.Instance)
            .SendAsync(ChatNotifier.Message(Chat("seeker-1"), toWorker: true, Target()), CancellationToken.None);

        dead.Should().BeEmpty();
    }
}
