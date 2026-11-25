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

    public List<ProfessionalListing> Published { get; } = [];

    /// <summary>What every search page answers (the server ranks; this fake only hands it back).</summary>
    public SearchResult SearchResults { get; set; } = SearchResult.Empty;

    public List<(ProfessionalSearchQuery Query, int Skip)> Searches { get; } = [];

    public Dictionary<string, ProfessionalListing> ById { get; } = [];

    public List<string> Removed { get; } = [];

    /// <summary>What <see cref="GetOwnDraftAsync"/> answers per uid (a worker's live listing on the server).</summary>
    public Dictionary<string, ProfessionalDraft> OwnDrafts { get; } = [];

    /// <summary>Thrown by <see cref="GetOwnDraftAsync"/> (a broken adapter), to test that launch never breaks on it.</summary>
    public Exception? OwnDraftThrows { get; set; }

    public List<string> OwnDraftReads { get; } = [];

    public Task<ProfessionalDraft?> GetOwnDraftAsync(string uid)
    {
        OwnDraftReads.Add(uid);
        return OwnDraftThrows is { } error ? Task.FromException<ProfessionalDraft?>(error) : Task.FromResult(OwnDrafts.GetValueOrDefault(uid));
    }

    public Task<SearchResult> SearchAsync(ProfessionalSearchQuery query, int skip)
    {
        Searches.Add((query, skip));
        return Task.FromResult(SearchResults);
    }

    public Task<ProfessionalListing?> GetByIdAsync(string professionalId) =>
        Task.FromResult(ById.GetValueOrDefault(professionalId));

    /// <summary>Thrown by the next publishes (e.g. a lost connection), after <see cref="LandsAnyway"/> is applied.</summary>
    public Exception? PublishThrows { get; set; }

    /// <summary>With <see cref="PublishThrows"/>: the server stored the listing, only its answer was lost.</summary>
    public bool LandsAnyway { get; set; }

    public Task<PublishResult> PublishAsync(ProfessionalListing listing)
    {
        Published.Add(listing);
        if (PublishThrows is not null)
        {
            if (LandsAnyway)
            {
                ById[listing.Id] = listing;
            }

            throw PublishThrows;
        }

        return Task.FromResult(NextResult);
    }

    public Task RemoveAsync(string professionalId)
    {
        Removed.Add(professionalId);
        return Task.CompletedTask;
    }

    /// <summary>Listings whose kept answers were dropped (after a block).</summary>
    public List<string> Forgotten { get; } = [];

    public void Forget(string professionalId) => Forgotten.Add(professionalId);
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

    public Task<AuthResult> SignInWithAppleAsync() => throw new NotSupportedException();

    /// <summary>True once Delete account reached the sign-in provider (it then signs out).</summary>
    public bool AccountDeleted { get; private set; }

    public Task DeleteAccountAsync()
    {
        AccountDeleted = true;
        User = null;
        return Task.CompletedTask;
    }

    public Task<AuthResult> SetDisplayNameAsync(string displayName) => throw new NotSupportedException();

    public Task SignOutAsync()
    {
        User = null;
        return Task.CompletedTask;
    }
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

    /// <summary>How many photos went up as selfies (checked, not re-encoded, on the real server).</summary>
    public int SelfiesSaved { get; private set; }

    public Task<string> SaveSelfieAsync(byte[] bytes)
    {
        SelfiesSaved++;
        return SaveAsync(bytes);
    }

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

    /// <summary>A selfie the camera took while the app was closed, handed back once by <see cref="RecoverSelfieAsync"/>.</summary>
    public PhotoPick? Recovered { get; set; }

    public Task<PhotoPick?> RecoverSelfieAsync()
    {
        var pick = Recovered;
        Recovered = null;
        return Task.FromResult(pick);
    }

    /// <summary>How many times the camera was opened for a selfie.</summary>
    public int Selfies { get; private set; }

    public Task<PhotoPick> TakeSelfieAsync()
    {
        Selfies++;
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

    /// <summary>When set, <see cref="GetAsync"/> throws it: no answer (an <c>HttpRequestException</c>) or a server error.</summary>
    public Exception? GetThrows { get; set; }

    /// <summary>The uids whose profile was read.</summary>
    public List<string> Reads { get; } = [];

    public Task<UserProfile?> GetAsync(string uid)
    {
        Reads.Add(uid);
        return GetThrows is { } error ? Task.FromException<UserProfile?>(error) : Task.FromResult(Profiles.GetValueOrDefault(uid));
    }

    public Task SaveDeviceAsync(string token, string platform)
    {
        if (FailSaves)
        {
            throw new HttpRequestException("offline");
        }

        Devices.Add((token, platform));
        return Task.CompletedTask;
    }

    /// <summary>When set, profile saves wait for it: a slow or cold server.</summary>
    public TaskCompletionSource? HoldSaves { get; set; }

    public async Task SaveAsync(UserProfile profile)
    {
        if (HoldSaves is not null)
        {
            await HoldSaves.Task;
        }

        if (FailSaves)
        {
            throw new HttpRequestException("offline");
        }

        // Like the server (UserStore.SaveAsync): the first save's time stays, whatever the app sends.
        Profiles[profile.Uid] = Profiles.TryGetValue(profile.Uid, out var saved) ? profile with { CreatedAt = saved.CreatedAt } : profile;
    }

    public Task DeleteAsync(string uid)
    {
        Deleted.Add(uid);
        Profiles.Remove(uid);
        return Task.CompletedTask;
    }

    /// <summary>How many times Delete account reached the server.</summary>
    public int AccountsDeleted { get; private set; }

    /// <summary>When set, Delete account fails like an offline phone.</summary>
    public bool FailAccountDelete { get; set; }

    public Task<bool> DeleteAccountAsync()
    {
        if (FailAccountDelete)
        {
            throw new HttpRequestException("offline");
        }

        AccountsDeleted++;
        return Task.FromResult(true);
    }
}

/// <summary>Reports and blocks sent to the server. Set <see cref="Fail"/> to simulate being offline.</summary>
public sealed class FakeSafety : ISafetyRepository
{
    public List<UserReport> Reports { get; } = [];

    public List<string> Blocked { get; } = [];

    public bool Fail { get; set; }

    public Task ReportAsync(UserReport report)
    {
        if (Fail)
        {
            throw new HttpRequestException("offline");
        }

        Reports.Add(report);
        if (report.AlsoBlock)
        {
            Blocked.Add(report.PersonId);
        }

        return Task.CompletedTask;
    }

    public Task BlockAsync(string personId)
    {
        if (Fail)
        {
            throw new HttpRequestException("offline");
        }

        Blocked.Add(personId);
        return Task.CompletedTask;
    }
}

/// <summary>
/// The feedback board on a server: <see cref="Posts"/> in the board's order, served a page at a time. Set
/// <see cref="Fail"/> to make every write fail like a phone with no signal.
/// </summary>
public sealed class FakeFeedback : IFeedbackRepository
{
    public List<FeedbackPost> Posts { get; } = [];

    public List<(string Title, string? Details, UserRole? Role)> Added { get; } = [];

    public List<long> Reported { get; } = [];

    public bool Fail { get; set; }

    public Task<FeedbackPage> GetPageAsync(int skip) =>
        Task.FromResult(new FeedbackPage([.. Posts.Skip(skip).Take(SideGigPh.Contracts.Paging.Feedback)], Posts.Count > skip + SideGigPh.Contracts.Paging.Feedback));

    public Task<IReadOnlyList<FeedbackPost>> FindSimilarAsync(string text) =>
        Task.FromResult<IReadOnlyList<FeedbackPost>>([.. Posts.Where(p => p.Title.Contains(text, StringComparison.OrdinalIgnoreCase))]);

    public Task<FeedbackPost> AddAsync(string title, string? details, UserRole? role)
    {
        ThrowIfOffline();
        Added.Add((title, details, role));
        var post = new FeedbackPost(Posts.Count + 1000, title, details, 1, FeedbackStatus.Open, VotedByMe: true, Mine: true, DateTimeOffset.UnixEpoch);
        Posts.Add(post);
        return Task.FromResult(post);
    }

    public Task<int> VoteAsync(long postId, bool on)
    {
        ThrowIfOffline();
        var index = Posts.FindIndex(p => p.Id == postId);
        Posts[index] = Posts[index] with { VotedByMe = on, Votes = Posts[index].Votes + (on ? 1 : -1) };
        return Task.FromResult(Posts[index].Votes);
    }

    public Task RemoveAsync(long postId)
    {
        ThrowIfOffline();
        Posts.RemoveAll(p => p.Id == postId);
        return Task.CompletedTask;
    }

    public Task ReportAsync(long postId)
    {
        ThrowIfOffline();
        Reported.Add(postId);
        return Task.CompletedTask;
    }

    private void ThrowIfOffline()
    {
        if (Fail)
        {
            throw new HttpRequestException("offline");
        }
    }
}

/// <summary>The usage events waiting on the phone, in memory: <see cref="Saved"/> is what was last written.</summary>
public sealed class FakePendingEvents : IPendingEventStore
{
    public PendingEventsState? Saved { get; set; }

    public int Saves { get; private set; }

    public Task<PendingEventsState?> LoadAsync() => Task.FromResult(Saved);

    public Task SaveAsync(PendingEventsState state)
    {
        Saves++;
        Saved = new PendingEventsState { Events = [.. state.Events] };
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

    public List<(string Number, string Message)> Texted { get; } = [];

    public Task<bool> TextAsync(string phoneNumber, string message)
    {
        Texted.Add((phoneNumber, message));
        return Task.FromResult(Opens);
    }
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

/// <summary>The phone's error log file, in memory.</summary>
public sealed class FakeErrorLogStore : IErrorLogStore
{
    public DiagnosticsState? Saved { get; set; }

    public int Saves { get; private set; }

    public Task<DiagnosticsState?> LoadAsync() => Task.FromResult(Saved);

    public Task SaveAsync(DiagnosticsState state)
    {
        Saves++;
        Saved = state;
        return Task.CompletedTask;
    }
}

/// <summary>The error reports API: records each batch; set <see cref="Answer"/> to refuse, or <see cref="Fail"/> to throw.</summary>
public sealed class FakeErrorReportRepository : IErrorReportRepository
{
    public List<IReadOnlyList<ErrorReport>> Batches { get; } = [];

    public bool Answer { get; set; } = true;

    public bool Fail { get; set; }

    public Task<bool> SendAsync(string installId, DeviceStamp device, IReadOnlyList<ErrorReport> reports)
    {
        if (Fail)
        {
            throw new HttpRequestException("offline");
        }

        Batches.Add(reports);
        return Task.FromResult(Answer);
    }
}

/// <summary>Job posts kept in memory: what the household posted, and a feed a test seeds for the worker.</summary>
public sealed class FakeJobPosts : IJobPostRepository
{
    private readonly List<JobPost> _mine = [];
    private readonly List<JobPost> _feed = [];

    public List<JobPostDraft> Added { get; } = [];

    public List<long> Reported { get; } = [];

    /// <summary>Every write throws, as with no connection.</summary>
    public bool Fail { get; set; }

    public bool HasListing { get; set; } = true;

    /// <summary>Puts posts in the worker's feed, in this order.</summary>
    public void Seed(params JobPost[] posts) => _feed.AddRange(posts);

    public Task<JobPost> AddAsync(JobPostDraft draft)
    {
        ThrowIfOffline();
        Added.Add(draft);
        var post = new JobPost(_mine.Count + 1000, draft.CategoryId!, draft.ServiceId!, draft.Description!.Trim(), draft.BudgetPesos!.Value, draft.CityId!,
            [.. draft.Photos.Select(photo => photo.Url)], IsOpen: true, Mine: true, OffersService: false, DistanceKm: null, DateTimeOffset.UnixEpoch, Title: draft.Title?.Trim());
        _mine.Insert(0, post);
        return Task.FromResult(post);
    }

    public List<(long PostId, JobPostDraft Draft)> Updated { get; } = [];

    public Task<JobPost> UpdateAsync(long postId, JobPostDraft draft)
    {
        ThrowIfOffline();
        var index = _mine.FindIndex(p => p.Id == postId);
        if (index < 0 || !_mine[index].IsOpen)
        {
            throw new HttpRequestException("gone", null, System.Net.HttpStatusCode.NotFound);
        }

        Updated.Add((postId, draft));
        _mine[index] = _mine[index] with
        {
            CategoryId = draft.CategoryId!, ServiceId = draft.ServiceId!, Description = draft.Description!.Trim(), Title = draft.Title?.Trim(),
            BudgetPesos = draft.BudgetPesos!.Value, BudgetMinPesos = draft.BudgetMinPesos, CityId = draft.CityId!, BarangayId = draft.BarangayId,
            PhotoUrls = [.. draft.Photos.Select(photo => photo.Url)], PhotoIds = [.. draft.Photos.Select(photo => photo.Id)],
        };
        return Task.FromResult(_mine[index]);
    }

    /// <summary>Puts a post in the household's own list (as if it came from the server).</summary>
    public void SeedMine(JobPost post) => _mine.Insert(0, post);

    public Task<IReadOnlyList<JobPost>> GetMineAsync() => Task.FromResult<IReadOnlyList<JobPost>>([.. _mine]);

    public Task CloseAsync(long postId)
    {
        ThrowIfOffline();
        var index = _mine.FindIndex(p => p.Id == postId);
        _mine[index] = _mine[index] with { IsOpen = false };
        return Task.CompletedTask;
    }

    public Task<JobPostFeed> GetFeedAsync(int skip) => Task.FromResult(new JobPostFeed(
        HasListing ? [.. _feed.Skip(skip).Take(SideGigPh.Contracts.Paging.JobPosts)] : [],
        HasListing && _feed.Count > skip + SideGigPh.Contracts.Paging.JobPosts,
        HasListing));

    public Task ReportAsync(long postId)
    {
        ThrowIfOffline();
        Reported.Add(postId);
        _feed.RemoveAll(p => p.Id == postId);
        return Task.CompletedTask;
    }

    /// <summary>Offer writes answer 404, as when the post closed meanwhile.</summary>
    public bool Gone { get; set; }

    /// <summary>The offers sent: post id, price, note (a withdraw removes it).</summary>
    public Dictionary<long, (int Price, string? Note)> Offers { get; } = [];

    /// <summary>What <see cref="GetOffersAsync"/> answers, per post.</summary>
    public Dictionary<long, List<JobOffer>> OffersOn { get; } = [];

    public List<(long PostId, long OfferId)> Picked { get; } = [];

    public Task SendOfferAsync(long postId, int pricePesos, string? note)
    {
        ThrowIfOfflineOrGone();
        Offers[postId] = (pricePesos, note);
        return Task.CompletedTask;
    }

    public Task WithdrawOfferAsync(long postId)
    {
        ThrowIfOfflineOrGone();
        Offers.Remove(postId);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<JobOffer>> GetOffersAsync(long postId) =>
        Task.FromResult<IReadOnlyList<JobOffer>>(OffersOn.TryGetValue(postId, out var offers) ? [.. offers] : []);

    /// <summary>The numbers <see cref="GetContactNumberAsync"/> gives, by the other person's uid (a pick linked them).</summary>
    public Dictionary<string, string> Numbers { get; } = [];

    public Task<string?> GetContactNumberAsync(string otherUid) => Task.FromResult(Numbers.GetValueOrDefault(otherUid));

    /// <summary>The picked jobs <see cref="GetPickedJobsAsync"/> gives, by the other person's uid.</summary>
    public Dictionary<string, List<PickedJob>> PickedWith { get; } = [];

    /// <summary>The household ratings sent: household uid, stars, comment.</summary>
    public List<(string SeekerUid, int Rating, string? Comment)> HouseholdRatings { get; } = [];

    public Task<IReadOnlyList<PickedJob>> GetPickedJobsAsync(string otherUid) =>
        Task.FromResult<IReadOnlyList<PickedJob>>(PickedWith.TryGetValue(otherUid, out var jobs) ? [.. jobs] : []);

    public Task<PickedJob> MarkDoneAsync(long postId)
    {
        ThrowIfOfflineOrGone();
        foreach (var jobs in PickedWith.Values)
        {
            var index = jobs.FindIndex(j => j.PostId == postId);
            if (index >= 0)
            {
                jobs[index] = jobs[index].IAmTheHousehold ? jobs[index] with { DoneByHousehold = true } : jobs[index] with { DoneByWorker = true };
                return Task.FromResult(jobs[index]);
            }
        }

        throw new InvalidOperationException($"No picked job on post {postId}.");
    }

    /// <summary>The picked jobs called off: post, reason, note.</summary>
    public List<(long PostId, string Reason, string? Note)> Cancelled { get; } = [];

    /// <summary>The places sent with <see cref="ShareMeetLocationAsync"/>: post id and point.</summary>
    public List<(long PostId, GeoPoint Point)> MeetLocations { get; } = [];

    public Task<PickedJob> ShareMeetLocationAsync(long postId, GeoPoint point)
    {
        ThrowIfOfflineOrGone();
        foreach (var jobs in PickedWith.Values)
        {
            var index = jobs.FindIndex(j => j.PostId == postId);
            if (index >= 0)
            {
                MeetLocations.Add((postId, point));
                jobs[index] = jobs[index] with { MeetPoint = point, MeetSharedAt = DateTimeOffset.UtcNow };
                return Task.FromResult(jobs[index]);
            }
        }

        throw new HttpRequestException("not found", null, System.Net.HttpStatusCode.NotFound);
    }

    public Task CancelPickAsync(long postId, string reason, string? note)
    {
        ThrowIfOfflineOrGone();
        foreach (var jobs in PickedWith.Values)
        {
            var index = jobs.FindIndex(j => j.PostId == postId);
            if (index >= 0)
            {
                if (jobs[index].IsCompleted)
                {
                    throw new HttpRequestException("completed", null, System.Net.HttpStatusCode.Conflict);
                }

                jobs.RemoveAt(index);
                Cancelled.Add((postId, reason, note));
                return Task.CompletedTask;
            }
        }

        throw new HttpRequestException("gone", null, System.Net.HttpStatusCode.NotFound);
    }

    public Task RateHouseholdAsync(string seekerUid, int rating, string? comment)
    {
        ThrowIfOfflineOrGone();
        HouseholdRatings.Add((seekerUid, rating, comment));
        return Task.CompletedTask;
    }

    public Task<JobOffer> PickOfferAsync(long postId, long offerId)
    {
        ThrowIfOfflineOrGone();
        Picked.Add((postId, offerId));
        return Task.FromResult(OffersOn[postId].Single(o => o.Id == offerId) with { IsPicked = true });
    }

    private void ThrowIfOfflineOrGone()
    {
        ThrowIfOffline();
        if (Gone)
        {
            throw new HttpRequestException("gone", null, System.Net.HttpStatusCode.NotFound);
        }
    }

    private void ThrowIfOffline()
    {
        if (Fail)
        {
            throw new HttpRequestException("offline");
        }
    }

    /// <summary>What <see cref="GetMyOffersAsync"/> answers.</summary>
    public List<MyOffer> MyOffers { get; } = [];

    public Task<IReadOnlyList<MyOffer>> GetMyOffersAsync() => Task.FromResult<IReadOnlyList<MyOffer>>(MyOffers);
}
