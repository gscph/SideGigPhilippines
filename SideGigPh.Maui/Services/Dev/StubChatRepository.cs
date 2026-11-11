using Microsoft.Extensions.Logging;
using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services.Dev;

/// <summary>
/// Stand-in until Firestore is wired. Keeps chats and messages in files on this phone
/// (<c>dev-chats.json</c>, <c>dev-chat-messages.json</c>), so both Inboxes survive a restart.
/// When <see cref="DevSamples.Enabled"/>, the pro's own Inbox also gets SAMPLE chats; they live in
/// memory only (ids start with <c>sample-</c>) and are never written to the files.
/// <see cref="Watch"/> only sees messages sent in this app, where Firestore pushes everyone's.
/// </summary>
public sealed class StubChatRepository(ILogger<StubChatRepository> logger) : IChatRepository
{
    private const string SamplePrefix = "sample-";

    private readonly JsonFile<List<Chat>> _chatFile = new("dev-chats.json", DevJsonContext.Default.ListChat, logger);
    private readonly JsonFile<List<ChatMessage>> _messageFile = new("dev-chat-messages.json", DevJsonContext.Default.ListChatMessage, logger);
    private readonly Dictionary<string, List<Action>> _watchers = [];
    private List<Chat>? _chats;
    private List<ChatMessage>? _messages;

    public async Task<IReadOnlyList<Chat>> GetInboxAsync(string personId, ChatSide side)
    {
        await LoadAsync();
        return _chats!
            .Where(chat => chat.PersonOn(side) == personId)
            .OrderByDescending(chat => chat.LastMessage.At)
            .ToList();
    }

    public async Task<Chat?> GetAsync(string chatId)
    {
        await LoadAsync();
        return _chats!.FirstOrDefault(chat => chat.Id == chatId);
    }

    public async Task<IReadOnlyList<ChatMessage>> GetMessagesAsync(string chatId)
    {
        await LoadAsync();
        return _messages!.Where(m => m.ChatId == chatId).OrderBy(m => m.At).ToList();
    }

    public async Task<Chat> SendAsync(Chat chat, ChatMessage message)
    {
        await LoadAsync();
        var current = _chats!.FirstOrDefault(c => c.Id == chat.Id) ?? chat with { CreatedAt = message.At };
        var saved = current.SeekerId == message.SenderId
            ? current with { LastMessage = message, SeekerReadAt = message.At }
            : current with { LastMessage = message, ProfessionalReadAt = message.At };

        _chats!.RemoveAll(c => c.Id == chat.Id);
        _chats.Add(saved);
        _messages!.Add(message);
        await WriteAsync();
        Notify(chat.Id);
        return saved;
    }

    public async Task MarkReadAsync(string chatId, ChatSide side)
    {
        await LoadAsync();
        var index = _chats!.FindIndex(c => c.Id == chatId);
        if (index < 0 || !_chats[index].IsUnreadFor(side))
        {
            return;
        }

        var now = DateTimeOffset.Now;
        _chats[index] = side == ChatSide.Seeker ? _chats[index] with { SeekerReadAt = now } : _chats[index] with { ProfessionalReadAt = now };
        await WriteAsync();
    }

    public IDisposable Watch(string chatId, Action changed)
    {
        lock (_watchers)
        {
            if (!_watchers.TryGetValue(chatId, out var list))
            {
                _watchers[chatId] = list = [];
            }

            list.Add(changed);
        }

        return new Unwatch(() =>
        {
            lock (_watchers)
            {
                _watchers[chatId].Remove(changed);
            }
        });
    }

    public async Task DeleteForPersonAsync(string personId)
    {
        await LoadAsync();
        var gone = _chats!.Where(c => c.SeekerId == personId || c.ProfessionalId == personId).Select(c => c.Id).ToHashSet();
        if (gone.Count == 0)
        {
            return;
        }

        _chats!.RemoveAll(c => gone.Contains(c.Id));
        _messages!.RemoveAll(m => gone.Contains(m.ChatId));
        await WriteAsync();

        // Next read brings the samples back, as on a fresh install.
        _chats = null;
        _messages = null;
    }

    private async Task LoadAsync()
    {
        if (_chats is not null)
        {
            return;
        }

        _messages = await _messageFile.ReadAsync() ?? [];
        _chats = await _chatFile.ReadAsync() ?? [];
        if (DevSamples.Enabled)
        {
            AddSamples(_chats, _messages);
        }
    }

    private async Task WriteAsync()
    {
        await _chatFile.WriteAsync(_chats!.Where(c => !c.Id.StartsWith(SamplePrefix, StringComparison.Ordinal)).ToList());
        await _messageFile.WriteAsync(_messages!.Where(m => !m.ChatId.StartsWith(SamplePrefix, StringComparison.Ordinal)).ToList());
    }

    private void Notify(string chatId)
    {
        Action[] watchers;
        lock (_watchers)
        {
            watchers = _watchers.TryGetValue(chatId, out var list) ? [.. list] : [];
        }

        foreach (var changed in watchers)
        {
            changed();
        }
    }

    /// <summary>Three customers writing to the pro's own listing, so the Pro Inbox can be seen filled in.</summary>
    private static void AddSamples(List<Chat> chats, List<ChatMessage> messages)
    {
        var pro = ListingMapper.OwnListingId;
        var now = DateTimeOffset.Now;

        void Add(string n, string seekerName, string serviceId, string cityId, string urgencyId, string budgetId,
                 params (bool FromSeeker, ChatMessageKind Kind, string? Text, DateTimeOffset At)[] lines)
        {
            var chatId = $"{SamplePrefix}{n}";
            var seekerId = $"{SamplePrefix}seeker-{n}";
            var thread = lines.Select((line, i) => new ChatMessage(
                $"{chatId}-{i}", chatId, line.FromSeeker ? seekerId : pro, line.Kind, line.Text, line.At)).ToList();
            messages.AddRange(thread);
            chats.Add(new Chat(chatId, pro, seekerId, string.Empty, seekerName, serviceId, cityId, urgencyId, budgetId,
                thread[^1], thread[^1].At, null, thread[0].At));
        }

        Add("1", "Maricel D.", "aircon-cleaning", "taytay", ReferenceData.UrgencyToday, "500-1000",
            (true, ChatMessageKind.Text, "Hi po! Nakita ko kayo sa SideGigPh. Kailangan ko po: Pa-linis ng aircon, dito sa Taytay, ngayong araw. Available po ba kayo?", now.AddMinutes(-40)),
            (true, ChatMessageKind.Call, null, now.AddMinutes(-25)));
        Add("2", "Jun R.", "aircon-repair", "angono", ReferenceData.UrgencyThisWeek, ReferenceData.Unsure,
            (true, ChatMessageKind.Text, "Hi po! Nakita ko kayo sa SideGigPh. Kailangan ko po: Hindi lumalamig / pa-repair ng aircon, dito sa Angono, ngayong linggo. Available po ba kayo?", now.AddHours(-3)));
        Add("3", "Liza P.", "aircon-freon", "taytay", ReferenceData.UrgencyCanvassing, "1000-2000",
            (true, ChatMessageKind.Text, "Hi po! Nakita ko kayo sa SideGigPh. Kailangan ko po: Pa-freon ng aircon, dito sa Taytay. Magkano po ang singil ninyo?", now.AddDays(-2)),
            (false, ChatMessageKind.Text, "Hello po! ₱1,500 po ang freon, kasama na ang check-up. Kailan po kayo free?", now.AddDays(-2).AddHours(1)),
            (true, ChatMessageKind.Text, "Sige po, Sabado ng umaga. Salamat po!", now.AddDays(-2).AddHours(2)));
    }

    private sealed class Unwatch(Action dispose) : IDisposable
    {
        public void Dispose() => dispose();
    }
}
