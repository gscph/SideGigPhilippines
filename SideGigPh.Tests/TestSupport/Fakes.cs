using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;

namespace SideGigPh.Tests.TestSupport;

// Simple stand-ins for the app's ports (server, photos, gallery, dialer, sign-in), so a unit test runs in
// milliseconds with no phone, network or Azure. Each one remembers what it was asked, so a test can check it,
// and can be told what to answer.

/// <summary>Keeps the onboarding answers in memory instead of a file.</summary>
public sealed class FakeOnboardingStore(OnboardingState state) : IOnboardingStore
{
    public OnboardingState State { get; private set; } = state;

    public int Saves { get; private set; }

    public bool Cleared { get; private set; }

    public Task<OnboardingState> LoadAsync() => Task.FromResult(State);

    public Task SaveAsync(OnboardingState state)
    {
        State = state;
        Saves++;
        return Task.CompletedTask;
    }

    public Task ClearAsync()
    {
        State = new OnboardingState();
        Cleared = true;
        return Task.CompletedTask;
    }
}

/// <summary>The listing server: answers publish with <see cref="NextResult"/> and remembers what was sent.</summary>
public sealed class FakeProfiles : IProfessionalProfileRepository
{
    public PublishResult NextResult { get; set; } = PublishResult.Published;

    public List<(ProfessionalListing Listing, IReadOnlyList<WorkReference> References)> Published { get; } = [];

    /// <summary>What every search page answers (the server ranks; this fake only hands it back).</summary>
    public SearchResult SearchResults { get; set; } = SearchResult.Empty;

    public List<(ProfessionalSearchQuery Query, int Skip)> Searches { get; } = [];

    public Dictionary<string, ProfessionalListing> ById { get; } = [];

    public List<string> Removed { get; } = [];

    public Task<bool> HasProfileAsync(string uid) => Task.FromResult(ById.ContainsKey(uid));

    public Task<SearchResult> SearchAsync(ProfessionalSearchQuery query, int skip)
    {
        Searches.Add((query, skip));
        return Task.FromResult(SearchResults);
    }

    public Task<ProfessionalListing?> GetByIdAsync(string professionalId) =>
        Task.FromResult(ById.GetValueOrDefault(professionalId));

    public Task<PublishResult> PublishAsync(ProfessionalListing listing, IReadOnlyList<WorkReference> references)
    {
        Published.Add((listing, references.ToList()));
        return Task.FromResult(NextResult);
    }

    public Task RemoveAsync(string professionalId)
    {
        Removed.Add(professionalId);
        return Task.CompletedTask;
    }
}

/// <summary>The signed-in account. Change <see cref="User"/> to simulate e.g. linking a phone later.</summary>
public sealed class FakeIdentity(AuthUser? user) : IIdentityProvider
{
    public AuthUser? User { get; set; } = user;

    /// <summary>None by default (no sign-in: no step asks for a phone); set it to test the phone checks.</summary>
    public AuthMethods Methods { get; set; } = AuthMethods.None;

    public Task<AuthUser?> GetCurrentUserAsync() => Task.FromResult(User);

    /// <summary>The ID token for a user. Integration tests swap in a real signed token.</summary>
    public Func<AuthUser, string> TokenFor { get; set; } = user => $"token-for-{user.Uid}";

    public Task<string?> GetIdTokenAsync() => Task.FromResult(User is null ? null : TokenFor(User));

    public Task<PhoneVerification> SignUpWithPhoneAsync(string phoneNumber) => throw new NotSupportedException();

    public Task<AuthResult> VerifyPhoneOtpAsync(string verificationId, string userEnteredCode) => throw new NotSupportedException();

    public Task<AuthResult> LinkPhoneAsync(string verificationId, string userEnteredCode) => throw new NotSupportedException();

    public Task<AuthResult> SignInWithGoogleAsync() => throw new NotSupportedException();

    public Task<AuthResult> SetDisplayNameAsync(string displayName) => throw new NotSupportedException();

    public Task SignOutAsync() => Task.CompletedTask;
}

/// <summary>
/// Photos "stored" in blob storage: the id is made from the first byte (so the same photo gets the same id, like
/// SHA-256), and every id has a URL like the real one. Set <see cref="Refuse"/> to make uploads fail.
/// </summary>
public sealed class FakePhotoStore : IPhotoStore
{
    public const string BaseUrl = "https://sidegigtest.blob.core.windows.net/photos/";

    /// <summary>When set, <see cref="SaveAsync"/> throws it (e.g. offline, or the photo belongs to another account).</summary>
    public PhotoStoreException? Refuse { get; set; }

    public List<string> Deleted { get; } = [];

    public Task<string> SaveAsync(byte[] bytes) =>
        Refuse is { } refusal ? throw refusal : Task.FromResult(TestData.PhotoId(bytes[0]));

    public Task<string?> GetUrlAsync(string photoId) => Task.FromResult<string?>(Url(photoId));

    public Task DeleteAsync(string photoId)
    {
        Deleted.Add(photoId);
        return Task.CompletedTask;
    }

    public static string Url(string photoId) => $"{BaseUrl}{photoId}.jpg";
}

/// <summary>The gallery: answers with <see cref="Next"/> (cancelled unless a test picks photos).</summary>
public sealed class FakePhotoPicker : IPhotoPicker
{
    public PhotoPick Next { get; set; } = PhotoPick.Cancelled;

    public int? LastMaxCount { get; private set; }

    /// <summary>The user picks photos whose first bytes are <paramref name="firstBytes"/> (so their ids are PhotoId(n)).</summary>
    public void Picks(params byte[] firstBytes) =>
        Next = new PhotoPick(PhotoPickStatus.Picked, firstBytes.Select(b => new[] { b, (byte)0xD8 }).ToList());

    public Task<PhotoPick> PickAsync(int maxCount)
    {
        LastMaxCount = maxCount;
        return Task.FromResult(Next);
    }

    public void OpenAppSettings()
    {
    }
}

/// <summary>Chats kept in memory: an Inbox per side, and what was sent and marked read.</summary>
public sealed class FakeChats : IChatRepository
{
    public List<Chat> Inbox { get; set; } = [];

    public int InboxReads { get; private set; }

    public List<(Chat Chat, ChatMessage Message)> Sent { get; } = [];

    public List<(string ChatId, ChatSide Side)> MarkedRead { get; } = [];

    public List<string> DeletedFor { get; } = [];

    public List<ChatMessage> Messages { get; set; } = [];

    public (string ChatId, string? LastMessageId)? Watching { get; private set; }

    /// <summary>Like the server: one page of <see cref="Inbox"/> (kept in the order given), after <paramref name="beforeChatId"/>.</summary>
    public Task<IReadOnlyList<Chat>> GetInboxAsync(string personId, ChatSide side, string? beforeChatId = null)
    {
        InboxReads++;
        var start = beforeChatId is null ? 0 : Inbox.FindIndex(c => c.Id == beforeChatId) + 1;
        return Task.FromResult<IReadOnlyList<Chat>>(start == 0 && beforeChatId is not null
            ? []
            : Inbox.Skip(start).Take(SideGigPh.Contracts.Paging.Inbox).ToList());
    }

    public Task<Chat?> GetAsync(string chatId) => Task.FromResult(Inbox.FirstOrDefault(c => c.Id == chatId));

    /// <summary>Like the server: the newest page before <paramref name="beforeMessageId"/>, oldest first.</summary>
    public Task<IReadOnlyList<ChatMessage>> GetMessagesAsync(string chatId, string? beforeMessageId = null)
    {
        var all = Messages.Where(m => m.ChatId == chatId).OrderBy(m => m.At).ToList();
        var end = beforeMessageId is null ? all.Count : all.FindIndex(m => m.Id == beforeMessageId);
        var start = Math.Max(0, end - SideGigPh.Contracts.Paging.Messages);
        return Task.FromResult<IReadOnlyList<ChatMessage>>(all.GetRange(start, Math.Max(0, end - start)));
    }

    public Task<Chat> SendAsync(Chat chat, ChatMessage message)
    {
        Sent.Add((chat, message));

        // Like the server: the time is stamped there, and the sender has read their own message.
        var saved = message with { At = message.At.AddSeconds(1) };
        return Task.FromResult(chat with { LastMessage = saved });
    }

    public Task MarkReadAsync(string chatId, ChatSide side)
    {
        MarkedRead.Add((chatId, side));
        return Task.CompletedTask;
    }

    public IDisposable Watch(string chatId, string? lastMessageId, Action<Chat> changed)
    {
        Watching = (chatId, lastMessageId);
        return new NoOp();
    }

    public Task DeleteForPersonAsync(string personId)
    {
        DeletedFor.Add(personId);
        return Task.CompletedTask;
    }

    private sealed class NoOp : IDisposable
    {
        public void Dispose()
        {
        }
    }
}

/// <summary>Reviews kept in memory.</summary>
public sealed class FakeReviews : IReviewRepository
{
    public List<Review> AboutPro { get; set; } = [];

    public List<Review> BySeeker { get; set; } = [];

    public int Reads { get; private set; }

    public List<Review> Saved { get; } = [];

    public List<string> DeletedFor { get; } = [];

    public Task<IReadOnlyList<Review>> GetForProfessionalAsync(string professionalId)
    {
        Reads++;
        return Task.FromResult<IReadOnlyList<Review>>(AboutPro);
    }

    public Task<IReadOnlyList<Review>> GetBySeekerAsync(string seekerId) => Task.FromResult<IReadOnlyList<Review>>(BySeeker);

    public Task SaveAsync(Review review)
    {
        Saved.Add(review);
        return Task.CompletedTask;
    }

    public Task DeleteForPersonAsync(string personId)
    {
        DeletedFor.Add(personId);
        return Task.CompletedTask;
    }
}

/// <summary>The account profile on the server. Set <see cref="FailSaves"/> to simulate being offline.</summary>
public sealed class FakeUsers : IUserRepository
{
    public Dictionary<string, UserProfile> Profiles { get; } = [];

    public bool FailSaves { get; set; }

    public List<string> Deleted { get; } = [];

    /// <summary>Phones registered for notifications, as (token, platform).</summary>
    public List<(string Token, string Platform)> Devices { get; } = [];

    public Task<UserProfile?> GetAsync(string uid) => Task.FromResult(Profiles.GetValueOrDefault(uid));

    public Task SaveDeviceAsync(string token, string platform)
    {
        if (FailSaves)
        {
            throw new HttpRequestException("offline");
        }

        Devices.Add((token, platform));
        return Task.CompletedTask;
    }

    public Task SaveAsync(UserProfile profile)
    {
        if (FailSaves)
        {
            throw new HttpRequestException("offline");
        }

        Profiles[profile.Uid] = profile;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string uid)
    {
        Deleted.Add(uid);
        Profiles.Remove(uid);
        return Task.CompletedTask;
    }
}

/// <summary>The phone's dialer: <see cref="Opens"/> says whether it opened.</summary>
public sealed class FakeContactLauncher : IContactLauncher
{
    public bool Opens { get; set; } = true;

    public List<string> Called { get; } = [];

    public Task<bool> CallAsync(string phoneNumber)
    {
        Called.Add(phoneNumber);
        return Task.FromResult(Opens);
    }

    public Task<bool> TextAsync(string phoneNumber, string message) => Task.FromResult(Opens);
}

/// <summary>
/// The phone's notifications: whether they're allowed, what the system dialog answers, the token, and what the app
/// asked the badge to show. <see cref="Arrive"/> and <see cref="Tap"/> play a notification coming in.
/// </summary>
public sealed class FakePush : IPushNotifications
{
    public string? Platform { get; set; } = "android";

    public bool Allowed { get; set; }

    /// <summary>What the person picks in the system dialog.</summary>
    public bool AllowsWhenAsked { get; set; } = true;

    public int Asked { get; private set; }

    public string? Token { get; set; } = "token-1";

    /// <summary>The tags the app last kept (the unread chats), or null before it said.</summary>
    public IReadOnlyCollection<string>? Shown { get; private set; }

    public event Action<string>? TokenChanged;

    public event Action<IReadOnlyDictionary<string, string>>? Received;

    public event Action<IReadOnlyDictionary<string, string>>? Tapped;

    public Task<bool> IsAllowedAsync() => Task.FromResult(Allowed);

    public Task<bool> RequestPermissionAsync()
    {
        Asked++;
        Allowed = AllowsWhenAsked;
        return Task.FromResult(Allowed);
    }

    public Task<string?> GetTokenAsync() => Task.FromResult(Allowed ? Token : null);

    public void ShowOnly(IReadOnlyCollection<string> unreadTags) => Shown = unreadTags;

    public void Arrive(IReadOnlyDictionary<string, string> data) => Received?.Invoke(data);

    public void Tap(IReadOnlyDictionary<string, string> data) => Tapped?.Invoke(data);

    public void ChangeToken(string token) => TokenChanged?.Invoke(token);
}

/// <summary>
/// The server's config: answers <see cref="Config"/> with <see cref="Version"/>, "unchanged" when the phone sends that
/// version, null when <see cref="Unreachable"/>; <see cref="Hang"/> never answers (until cancelled).
/// </summary>
public sealed class FakeConfigSource : IConfigSource<AppConfig>
{
    public AppConfig? Config { get; set; }

    public string? Version { get; set; } = "\"v1\"";

    public bool Unreachable { get; set; }

    public bool Hang { get; set; }

    public List<string?> AskedWith { get; } = [];

    public async Task<ConfigFetch<AppConfig>?> FetchAsync(string? savedVersion, CancellationToken cancellationToken)
    {
        AskedWith.Add(savedVersion);
        if (Hang)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }

        if (Unreachable || Config is null)
        {
            return null;
        }

        return savedVersion is not null && savedVersion == Version
            ? new ConfigFetch<AppConfig>(null, Version)
            : new ConfigFetch<AppConfig>(Config, Version);
    }
}

/// <summary>The config file on the phone, in memory.</summary>
public sealed class FakeConfigCache : IConfigCache<AppConfig>
{
    public Stamped<AppConfig>? Saved { get; set; }

    public int Writes { get; private set; }

    public Task<Stamped<AppConfig>?> ReadAsync() => Task.FromResult(Saved);

    public Task WriteAsync(Stamped<AppConfig> saved)
    {
        Saved = saved;
        Writes++;
        return Task.CompletedTask;
    }
}

/// <summary>The server's "Others" notes: remembers them, or throws when <see cref="Fail"/>.</summary>
public sealed class FakeOtherJobNoteRepository : IOtherJobNoteRepository
{
    public bool Fail { get; set; }

    public List<OtherJobNote> Notes { get; } = [];

    public Task SaveAsync(OtherJobNote note)
    {
        if (Fail)
        {
            throw new HttpRequestException("offline");
        }

        Notes.Add(note);
        return Task.CompletedTask;
    }
}

/// <summary>A clock the test sets (timers still run in real time).</summary>
public sealed class FakeClock(DateTimeOffset now) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = now;

    public override DateTimeOffset GetUtcNow() => Now;
}
