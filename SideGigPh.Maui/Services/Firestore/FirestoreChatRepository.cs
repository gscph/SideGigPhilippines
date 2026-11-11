using Microsoft.Extensions.Logging;
using Plugin.Firebase.Firestore;
using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services.Firestore;

/// <summary>
/// <see cref="IChatRepository"/> on <c>chats/{proId}_{seekerUid}</c> and its <c>messages</c> subcollection.
/// Each Inbox is one query (<c>seekerUid == me</c> or <c>professionalId == me</c>, newest first), which the rules
/// and the two composite indexes in <c>firebase/firestore.indexes.json</c> are built for. Sending a message is one
/// batch: the message, and the chat with it as the last message (which also creates the chat the first time).
/// </summary>
public sealed class FirestoreChatRepository(ILogger<FirestoreChatRepository> logger) : IChatRepository
{
    public Task<IReadOnlyList<Chat>> GetInboxAsync(string personId, ChatSide side) => FirestoreAccess.ReadAsync<IReadOnlyList<Chat>>(async () =>
    {
        var snapshot = await FirestoreAccess.Db.GetCollection(FirestoreAccess.Chats)
            .WhereEqualsTo(side == ChatSide.Seeker ? "seekerUid" : "professionalId", personId)
            .OrderBy("lastMessageAt", descending: true)
            .GetDocumentsAsync<ChatDoc>();
        return snapshot.Documents.Select(d => ToChat(d.Data.Id ?? string.Empty, d.Data)).ToList();
    }, [], logger, $"the {side} Inbox of {personId}");

    public Task<Chat?> GetAsync(string chatId) => FirestoreAccess.ReadAsync(async () =>
    {
        var snapshot = await ChatDocument(chatId).GetDocumentSnapshotAsync<ChatDoc>();
        return snapshot.Data is { } doc ? ToChat(chatId, doc) : null;
    }, null, logger, $"chat {chatId}");

    public Task<IReadOnlyList<ChatMessage>> GetMessagesAsync(string chatId) => FirestoreAccess.ReadAsync<IReadOnlyList<ChatMessage>>(async () =>
    {
        var snapshot = await ChatDocument(chatId).GetCollection(FirestoreAccess.Messages)
            .OrderBy("at", descending: false)
            .GetDocumentsAsync<MessageDoc>();
        return snapshot.Documents.Select(d => ToMessage(chatId, d.Data)).ToList();
    }, [], logger, $"the messages of chat {chatId}");

    public async Task<Chat> SendAsync(Chat chat, ChatMessage message)
    {
        try
        {
            var document = ChatDocument(chat.Id);

            // The first message creates the chat from `chat` (names, job); later ones keep what's stored.
            var current = (await document.GetDocumentSnapshotAsync<ChatDoc>()).Data;
            var saved = current is null
                ? chat with { CreatedAt = message.At }
                : ToChat(chat.Id, current);
            saved = message.SenderId == saved.SeekerId
                ? saved with { LastMessage = message, SeekerReadAt = message.At }
                : saved with { LastMessage = message, ProfessionalReadAt = message.At };

            // One batch, because the rules check the message and the chat's new last message together.
            var batch = FirestoreAccess.Db.CreateBatch();
            batch.SetData(document.GetCollection(FirestoreAccess.Messages).GetDocument(message.Id), ToDoc(message));
            batch.SetData(document, ToDoc(saved));
            await batch.CommitAsync();
            return saved;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Firestore: couldn't send a message in chat {ChatId}.", chat.Id);
            throw;
        }
    }

    public async Task MarkReadAsync(string chatId, ChatSide side)
    {
        try
        {
            var document = ChatDocument(chatId);
            if ((await document.GetDocumentSnapshotAsync<ChatDoc>()).Data is not { } current)
            {
                return;
            }

            // Only this side's read time changes: that is all the rules allow here.
            var chat = ToChat(chatId, current);
            var now = DateTimeOffset.Now;
            await document.SetDataAsync(ToDoc(side == ChatSide.Seeker ? chat with { SeekerReadAt = now } : chat with { ProfessionalReadAt = now }));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Being denied because a new message arrived at the same moment is harmless: the next open marks it read.
            logger.LogWarning(ex, "Firestore: couldn't mark chat {ChatId} read.", chatId);
        }
    }

    /// <summary>Live updates: calls <paramref name="changed"/> when a message arrives or is sent, but not for the messages already there.</summary>
    public IDisposable Watch(string chatId, Action changed)
    {
        var first = true;
        return ChatDocument(chatId).GetCollection(FirestoreAccess.Messages).AddSnapshotListener<MessageDoc>(
            _ =>
            {
                // The first callback is the current content, which the page has just read.
                if (first)
                {
                    first = false;
                    return;
                }

                changed();
            },
            ex => logger.LogError(ex, "Firestore: the live view of chat {ChatId} stopped.", chatId));
    }

    public async Task DeleteForPersonAsync(string personId)
    {
        try
        {
            var chats = new List<IDocumentReference>();
            foreach (var field in new[] { "seekerUid", "professionalId" })
            {
                var snapshot = await FirestoreAccess.Db.GetCollection(FirestoreAccess.Chats).WhereEqualsTo(field, personId).GetDocumentsAsync<ChatDoc>();
                chats.AddRange(snapshot.Documents.Select(d => d.Reference));
            }

            foreach (var chat in chats)
            {
                // Messages first: the rules read the chat to decide who may delete them.
                var messages = await chat.GetCollection(FirestoreAccess.Messages).GetDocumentsAsync<MessageDoc>();
                foreach (var batch in messages.Documents.Select(d => d.Reference).Append(chat).Chunk(FirestoreAccess.BatchSize))
                {
                    var writes = FirestoreAccess.Db.CreateBatch();
                    foreach (var document in batch)
                    {
                        writes.DeleteDocument(document);
                    }

                    await writes.CommitAsync();
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Firestore: couldn't delete the chats of {PersonId}.", personId);
            throw;
        }
    }

    private static IDocumentReference ChatDocument(string chatId) => FirestoreAccess.Db.GetDocument($"{FirestoreAccess.Chats}/{chatId}");

    private static string KindName(ChatMessageKind kind) => kind == ChatMessageKind.Call ? "call" : "text";

    private static ChatMessageKind KindOf(string kind) => kind == "call" ? ChatMessageKind.Call : ChatMessageKind.Text;

    private static MessageDoc ToDoc(ChatMessage message) => new()
    {
        SenderUid = message.SenderId,
        Kind = KindName(message.Kind),
        Text = message.Kind == ChatMessageKind.Call ? null : FirestoreAccess.Limit(message.Text, 1000),
        At = message.At,
    };

    private static ChatDoc ToDoc(Chat chat) => new()
    {
        ProfessionalId = chat.ProfessionalId,
        SeekerUid = chat.SeekerId,
        ProfessionalName = FirestoreAccess.Name(chat.ProfessionalName),
        SeekerName = FirestoreAccess.Name(chat.SeekerName),
        ServiceId = chat.ServiceId,
        CityId = chat.CityId,
        UrgencyId = chat.UrgencyId,
        BudgetId = chat.BudgetId,
        LastMessage = new LastMessageDoc
        {
            Id = chat.LastMessage.Id,
            Text = chat.LastMessage.Kind == ChatMessageKind.Call ? null : FirestoreAccess.Limit(chat.LastMessage.Text, 1000),
            Kind = KindName(chat.LastMessage.Kind),
            SenderUid = chat.LastMessage.SenderId,
            At = chat.LastMessage.At,
        },
        LastMessageAt = chat.LastMessage.At,
        SeekerReadAt = FirestoreAccess.WriteTime(chat.SeekerReadAt),
        ProfessionalReadAt = FirestoreAccess.WriteTime(chat.ProfessionalReadAt),
        CreatedAt = chat.CreatedAt,
    };

    private static ChatMessage ToMessage(string chatId, MessageDoc doc) =>
        new(doc.Id ?? string.Empty, chatId, doc.SenderUid, KindOf(doc.Kind), doc.Text, doc.At);

    private static Chat ToChat(string chatId, ChatDoc doc) => new(
        chatId,
        doc.ProfessionalId,
        doc.SeekerUid,
        doc.ProfessionalName,
        doc.SeekerName,
        doc.ServiceId,
        doc.CityId,
        doc.UrgencyId,
        doc.BudgetId,
        new ChatMessage(doc.LastMessage.Id, chatId, doc.LastMessage.SenderUid, KindOf(doc.LastMessage.Kind), doc.LastMessage.Text, doc.LastMessage.At),
        FirestoreAccess.ReadTime(doc.SeekerReadAt),
        FirestoreAccess.ReadTime(doc.ProfessionalReadAt),
        doc.CreatedAt);
}
