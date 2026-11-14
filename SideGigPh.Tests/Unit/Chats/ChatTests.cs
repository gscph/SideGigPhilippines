using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;

namespace SideGigPh.Tests.Unit.Chats;

/// <summary>
/// The Inbox badge tells a worker a customer is waiting. A badge that never lights loses them the job; one that
/// never clears is noise. Also covers the money maths shown to workers when they set their earnings goal.
/// </summary>
public class ChatTests
{
    private static readonly DateTimeOffset Noon = new(2026, 11, 13, 12, 0, 0, TimeSpan.Zero);

    private static Chat ChatWithLastMessageFrom(string senderId, DateTimeOffset? proReadAt = null) => new(
        Chat.IdFor("pro", "seeker"), "pro", "seeker", "Pro", "Seeker", null, null, null, null,
        new ChatMessage("m1", "pro_seeker", senderId, ChatMessageKind.Text, "Hi", Noon),
        SeekerReadAt: null, ProfessionalReadAt: proReadAt, CreatedAt: Noon);

    [Fact]
    public void IdFor_IsProThenSeeker() => Chat.IdFor("pro", "seeker").Should().Be("pro_seeker");

    [Fact]
    public void CustomerWroteLast_NotOpened_IsUnreadForTheWorker() =>
        ChatWithLastMessageFrom("seeker").IsUnreadFor(ChatSide.Professional).Should().BeTrue();

    [Fact]
    public void CustomerWroteLast_OpenedAfterwards_IsRead() =>
        ChatWithLastMessageFrom("seeker", proReadAt: Noon.AddMinutes(1)).IsUnreadFor(ChatSide.Professional).Should().BeFalse();

    [Fact]
    public void CustomerWroteAgainAfterWorkerOpened_IsUnreadAgain() =>
        ChatWithLastMessageFrom("seeker", proReadAt: Noon.AddMinutes(-5)).IsUnreadFor(ChatSide.Professional).Should().BeTrue();

    [Fact]
    public void YourOwnMessage_IsNeverUnreadForYou() =>
        ChatWithLastMessageFrom("pro").IsUnreadFor(ChatSide.Professional).Should().BeFalse();

    [Fact]
    public void Peso_UsesThePesoSignAndThousandsCommas() => Money.Peso(12500).Should().Be("₱12,500");
}
