namespace SideGigPh.Maui.Models;

/// <summary>Which person in a chat: the Seeker who started it, or the Professional.</summary>
public enum ChatSide
{
    Seeker,
    Professional,
}

public enum ChatMessageKind
{
    Text,

    /// <summary>The sender tapped Call, which opened their phone's dialer. Logged so both sides see it.</summary>
    Call,

    /// <summary>
    /// "Customer's location": the household sent the picked worker its exact place (<c>Services/MeetLocation</c>). It
    /// holds no coordinates; a tap opens the place kept on the picked job, so it says so once that is gone.
    /// </summary>
    Pin,
}

/// <summary>One message in a <see cref="Chat"/>. <see cref="Text"/> is null for a <see cref="ChatMessageKind.Call"/> or <see cref="ChatMessageKind.Pin"/>.</summary>
public sealed record ChatMessage(
    string Id,
    string ChatId,
    string SenderId,
    ChatMessageKind Kind,
    string? Text,
    DateTimeOffset At);

/// <summary>
/// The one chat between a Seeker and a Professional (id <c>{professionalId}_{seekerId}</c>), started
/// by the Seeker's first message. It carries the job from the Seeker's answers (ids in
/// <c>ReferenceData</c>), both names and the last message, so an Inbox needs no other reads.
/// Unread = the other person sent the last message after this side last opened the chat.
/// </summary>
public sealed record Chat(
    string Id,
    string ProfessionalId,
    string SeekerId,
    string ProfessionalName,
    string SeekerName,
    string? ServiceId,
    string? CityId,
    string? UrgencyId,
    string? BudgetId,
    ChatMessage LastMessage,
    DateTimeOffset? SeekerReadAt,
    DateTimeOffset? ProfessionalReadAt,
    DateTimeOffset CreatedAt)
{
    public static string IdFor(string professionalId, string seekerId) => $"{professionalId}_{seekerId}";

    public string PersonOn(ChatSide side) => side == ChatSide.Seeker ? SeekerId : ProfessionalId;

    public bool IsUnreadFor(ChatSide side)
    {
        var readAt = side == ChatSide.Seeker ? SeekerReadAt : ProfessionalReadAt;
        return LastMessage.SenderId != PersonOn(side) && (readAt is null || LastMessage.At > readAt);
    }
}
