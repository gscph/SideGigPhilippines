using Microsoft.EntityFrameworkCore;
using SideGigPh.Contracts;

namespace SideGigPh.AzureSQL;

public enum SendOutcome
{
    Sent,

    /// <summary>The caller isn't in this chat, or a Skilled Worker tried to start one.</summary>
    Forbidden,

    /// <summary>The Skilled Worker's listing is gone, so a new chat can't start.</summary>
    NoListing,

    /// <summary>The Seeker has started too many chats lately (the caller's <c>canStartChat</c> said no).</summary>
    TooManyChats,

    Invalid,
}

/// <param name="IsNew">True when this call saved the message; false when it was already saved (a retry), so nobody is told twice.</param>
public sealed record SendResult(SendOutcome Outcome, ChatDto? Chat = null, bool IsNew = false);

/// <summary>
/// Chats between a Seeker and a Skilled Worker (<c>Chats</c>, id <c>{professionalId}_{seekerId}</c>) and their messages.
/// Only the two people in a chat can read or write it. A message and the chat's copy of the last message are
/// saved in one transaction.
/// </summary>
public sealed class ChatStore(SideGigPhDb db)
{
    public const int MaxText = 1000;

    /// <summary>The longest uid a chat can name (<c>Users.Uid</c>, varchar(128)).</summary>
    public const int MaxUid = 128;

    public static string IdFor(string professionalId, string seekerId) => $"{professionalId}_{seekerId}";

    /// <summary>The person's chats on one side, most recent message first.</summary>
    public async Task<IReadOnlyList<ChatDto>> InboxAsync(string uid, bool asSeeker, CancellationToken cancellationToken)
    {
        var chats = db.Chats.AsNoTracking().Where(c => asSeeker ? c.SeekerId == uid : c.ProfessionalId == uid);
        var rows = await chats.OrderByDescending(c => c.LastMessageAt).ToListAsync(cancellationToken);
        return rows.Select(ToDto).ToList();
    }

    /// <summary>The chat, or null when it doesn't exist or the caller isn't in it.</summary>
    public async Task<ChatDto?> GetAsync(string chatId, string uid, CancellationToken cancellationToken) =>
        await db.Chats.AsNoTracking().FirstOrDefaultAsync(c => c.Id == chatId && (c.SeekerId == uid || c.ProfessionalId == uid), cancellationToken)
            is { } row ? ToDto(row) : null;

    /// <summary>The most messages one page may ask for.</summary>
    public const int MaxPage = 100;

    /// <summary>
    /// The chat's messages, oldest first, or null when the caller isn't in it. With <paramref name="take"/>, only the
    /// newest <paramref name="take"/> (at most <see cref="MaxPage"/>) that came before message
    /// <paramref name="beforeId"/>, or before now without it: the chat opens on its latest page and loads older ones
    /// as the person scrolls up. Without <paramref name="take"/> it is every message (app versions before paging).
    /// Messages sent in the same millisecond keep a fixed order by id.
    /// </summary>
    public async Task<IReadOnlyList<MessageDto>?> MessagesAsync(
        string chatId, string uid, int? take, string? beforeId, CancellationToken cancellationToken)
    {
        if (!await db.Chats.AnyAsync(c => c.Id == chatId && (c.SeekerId == uid || c.ProfessionalId == uid), cancellationToken))
        {
            return null;
        }

        var messages = db.ChatMessages.AsNoTracking().Where(m => m.ChatId == chatId);
        if (take is null)
        {
            return await messages.OrderBy(m => m.At).ThenBy(m => m.Id).Select(MessageToDto).ToListAsync(cancellationToken);
        }

        if (beforeId is not null)
        {
            var before = await messages.Where(m => m.Id == beforeId).Select(m => (DateTimeOffset?)m.At).FirstOrDefaultAsync(cancellationToken);
            if (before is not { } at)
            {
                return [];
            }

            messages = messages.Where(m => m.At < at || (m.At == at && string.Compare(m.Id, beforeId) < 0));
        }

        var newest = await messages
            .OrderByDescending(m => m.At).ThenByDescending(m => m.Id)
            .Take(Math.Clamp(take.Value, 1, MaxPage))
            .Select(MessageToDto)
            .ToListAsync(cancellationToken);
        newest.Reverse();
        return newest;
    }

    private static readonly System.Linq.Expressions.Expression<Func<ChatMessageRow, MessageDto>> MessageToDto =
        m => new MessageDto(m.Id, m.ChatId, m.SenderId, m.Kind, m.Text, m.At);

    /// <summary>
    /// Adds the caller's message and makes it the chat's last message (the sender has read it). The first message
    /// creates the chat, only the Seeker can send it, and only when <paramref name="canStartChat"/> agrees (a limit on
    /// new chats); the chat takes the worker's name from their listing, not the request. Sending the same message id
    /// again changes nothing.
    /// </summary>
    public async Task<SendResult> SendAsync(
        string chatId, string uid, SendMessageRequest request, Func<bool> canStartChat, CancellationToken cancellationToken)
    {
        var isCall = request.Kind == Wire.Call;
        var text = isCall ? null : StoreText.Limit(request.Text, MaxText);
        if (request.ProfessionalId is not { Length: > 0 and <= MaxUid } || request.SeekerId is not { Length: > 0 and <= MaxUid }
            || chatId != IdFor(request.ProfessionalId, request.SeekerId) || request.MessageId is not { Length: > 0 and <= 64 }
            || (request.Kind is not (Wire.Text or Wire.Call)) || (!isCall && text is null) || request.ProfessionalId == request.SeekerId)
        {
            return new SendResult(SendOutcome.Invalid);
        }

        if (uid != request.SeekerId && uid != request.ProfessionalId)
        {
            return new SendResult(SendOutcome.Forbidden);
        }

        return await db.InTransactionAsync(async ct =>
        {
            var chat = await db.Chats.FirstOrDefaultAsync(c => c.Id == chatId, ct);
            if (chat is not null && await db.ChatMessages.AnyAsync(m => m.Id == request.MessageId, ct))
            {
                return new SendResult(SendOutcome.Sent, ToDto(chat));
            }

            var now = DateTimeOffset.UtcNow;
            if (chat is null)
            {
                if (uid != request.SeekerId)
                {
                    return new SendResult(SendOutcome.Forbidden);
                }

                var professionalName = await db.Professionals
                    .Where(p => p.Id == request.ProfessionalId)
                    .Select(p => p.DisplayName)
                    .FirstOrDefaultAsync(ct);
                if (professionalName is null)
                {
                    return new SendResult(SendOutcome.NoListing);
                }

                if (!canStartChat())
                {
                    return new SendResult(SendOutcome.TooManyChats);
                }

                chat = new ChatRow
                {
                    Id = chatId,
                    ProfessionalId = request.ProfessionalId,
                    SeekerId = request.SeekerId,
                    ProfessionalName = StoreText.Name(professionalName, 60),
                    SeekerName = StoreText.Name(request.SeekerName, 60),
                    ServiceId = Code(request.ServiceId),
                    CityId = Code(request.CityId),
                    UrgencyId = Code(request.UrgencyId),
                    BudgetId = Code(request.BudgetId),
                    LastMessageId = request.MessageId,
                    LastMessageSenderId = uid,
                    LastMessageKind = request.Kind,
                    CreatedAt = now,
                };
                db.Chats.Add(chat);
            }

            db.ChatMessages.Add(new ChatMessageRow
            {
                Id = request.MessageId,
                ChatId = chatId,
                SenderId = uid,
                Kind = request.Kind,
                Text = text,
                At = now,
            });

            chat.LastMessageId = request.MessageId;
            chat.LastMessageSenderId = uid;
            chat.LastMessageKind = request.Kind;
            chat.LastMessageText = text;
            chat.LastMessageAt = now;
            if (uid == chat.SeekerId)
            {
                chat.SeekerReadAt = now;
            }
            else
            {
                chat.ProfessionalReadAt = now;
                chat.ProfessionalRepliedAt ??= now;  // their first reply: how responsive they are (search ranking)
            }

            await db.SaveChangesAsync(ct);
            return new SendResult(SendOutcome.Sent, ToDto(chat), IsNew: true);
        }, cancellationToken);
    }

    /// <summary>The caller has now read everything in the chat on that side. False when they aren't that side of it.</summary>
    public async Task<bool> MarkReadAsync(string chatId, string uid, bool asSeeker, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var changed = asSeeker
            ? await db.Chats.Where(c => c.Id == chatId && c.SeekerId == uid)
                .ExecuteUpdateAsync(set => set.SetProperty(c => c.SeekerReadAt, now), cancellationToken)
            : await db.Chats.Where(c => c.Id == chatId && c.ProfessionalId == uid)
                .ExecuteUpdateAsync(set => set.SetProperty(c => c.ProfessionalReadAt, now), cancellationToken);
        return changed > 0;
    }

    /// <summary>Deletes every chat the person is in, on either side (messages cascade).</summary>
    public Task DeleteForPersonAsync(string uid, CancellationToken cancellationToken) =>
        db.Chats.Where(c => c.SeekerId == uid || c.ProfessionalId == uid).ExecuteDeleteAsync(cancellationToken);

    private static string? Code(string? value) => StoreText.IsCode(value) ? value : null;

    private static ChatDto ToDto(ChatRow c) => new(
        c.Id,
        c.ProfessionalId,
        c.SeekerId,
        c.ProfessionalName,
        c.SeekerName,
        c.ServiceId,
        c.CityId,
        c.UrgencyId,
        c.BudgetId,
        new MessageDto(c.LastMessageId, c.Id, c.LastMessageSenderId, c.LastMessageKind, c.LastMessageText, c.LastMessageAt),
        c.SeekerReadAt,
        c.ProfessionalReadAt,
        c.CreatedAt);
}
