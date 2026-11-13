using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services.Localization;
using SideGigPh.Maui.Services.Search;

namespace SideGigPh.Maui.Services;

/// <summary>
/// Everything the Seeker's screens share: their current search (saved with their interview
/// answers), their Inbox (a chat per pro they contacted), their reviews, and the actions on
/// them. One instance for the app, so the tabs update when a page changes something
/// (<see cref="Changed"/>). Only ports are used here, never adapters.
/// </summary>
public sealed class SeekerWorkspace(
    IOnboardingStore store,
    ProfessionalSearch search,
    IProfessionalProfileRepository profiles,
    IChatRepository chats,
    IReviewRepository reviews,
    IContactLauncher phone,
    IIdentityProvider identity,
    IPhotoPicker picker,
    IPhotoStore photos,
    AppText text)
{
    /// <summary>The Seeker's id when nobody is signed in (no sign-in provider registered). With an account it is their uid: <see cref="MyId"/>.</summary>
    public const string LocalSeekerId = "me";

    /// <summary>How long the Inbox and reviews count as fresh: switching tabs within this doesn't ask the server again.</summary>
    public static readonly TimeSpan ActivityFreshFor = TimeSpan.FromSeconds(15);

    private OnboardingState? _state;
    private Task<bool>? _loading;
    private DateTimeOffset _refreshedAt = DateTimeOffset.MinValue;

    /// <summary>The Seeker's id in chats and reviews: the signed-in user's uid (set when the workspace loads), or <see cref="LocalSeekerId"/>.</summary>
    public string MyId { get; private set; } = LocalSeekerId;

    /// <summary>Raised after anything here changes, so the shell can re-render.</summary>
    public event Action? Changed;

    public SeekerAnswers Answers => _state?.Seeker ?? new SeekerAnswers();

    /// <summary>The Seeker's one profile photo (top bar avatar, Me page), or null.</summary>
    public string? ProfilePhotoUrl { get; private set; }

    /// <summary>What search results show: the interview answers, as changed on the results page since.</summary>
    public SearchRequest Request => new(Answers.CategoryId, Answers.ServiceId, Answers.CityId, Answers.BudgetId);

    /// <summary>One chat per pro the Seeker contacted, most recent message first.</summary>
    public IReadOnlyList<InboxThread> Threads { get; private set; } = [];

    /// <summary>Chats where the pro wrote last and the Seeker hasn't opened it since: the Inbox badge.</summary>
    public int UnreadCount => UnreadChatIds.Count;

    /// <summary>The unread chats' ids: the app icon's badge and which notifications stay in the tray.</summary>
    public IReadOnlyList<string> UnreadChatIds =>
        Threads.Where(thread => thread.Chat.IsUnreadFor(ChatSide.Seeker)).Select(thread => thread.Chat.Id).ToList();

    /// <summary>Reviews this Seeker wrote, newest first.</summary>
    public IReadOnlyList<Review> MyReviews { get; private set; } = [];

    /// <summary>Pros the Seeker contacted but hasn't reviewed yet.</summary>
    public IReadOnlyList<ProfessionalListing> ToRate =>
        Threads.Select(thread => thread.Pro).Where(pro => MyReviewOf(pro.Id) is null).ToList();

    /// <summary>
    /// Loads the saved answers and Inbox once. False if this user hasn't finished the Seeker
    /// interview, in which case the caller sends them back to <c>/</c>.
    /// </summary>
    public async Task<bool> EnsureLoadedAsync()
    {
        _loading ??= LoadAsync();
        var loaded = await _loading;
        if (!loaded)
        {
            _loading = null; // Try again once the interview is finished.
        }

        return loaded;
    }

    /// <summary>How the saved search words were understood (None when the job was picked by tapping).</summary>
    public SearchIntent CurrentIntent => SearchIntentParser.Parse(Answers.SearchText);

    public Task<SearchResult> SearchAsync() => search.SearchAsync(Request);

    /// <summary>
    /// Free-text search: works out the job from the words and searches for it. When nothing
    /// matches, the previous job is kept and the caller shows a "pick a service" hint.
    /// </summary>
    public async Task<SearchIntent> SearchForAsync(string? words)
    {
        var intent = SearchIntentParser.Parse(words);
        Answers.SearchText = string.IsNullOrWhiteSpace(words) ? null : words.Trim();
        if (intent.Matched)
        {
            Answers.CategoryId = intent.CategoryId;
            Answers.ServiceId = intent.ServiceId;
        }

        await SaveAsync();
        return intent;
    }

    /// <summary>A tapped category: all its jobs, and the typed words no longer apply.</summary>
    public Task PickCategoryAsync(string categoryId)
    {
        Answers.CategoryId = categoryId;
        Answers.ServiceId = null;
        Answers.SearchText = null;
        return SaveAsync();
    }

    /// <summary>A tapped job in the current category, or null for all of the category's jobs.</summary>
    public Task PickServiceAsync(string? serviceId)
    {
        Answers.ServiceId = serviceId;
        Answers.SearchText = null;
        return SaveAsync();
    }

    public Task PickTownAsync(string cityId)
    {
        Answers.CityId = cityId;
        Answers.AreaId = ReferenceData.AreaOf(cityId);
        return SaveAsync();
    }

    public Task PickBudgetAsync(string budgetId)
    {
        Answers.BudgetId = budgetId;
        return SaveAsync();
    }

    public Task<ProfessionalListing?> GetProAsync(string professionalId) => profiles.GetByIdAsync(professionalId);

    public Task<IReadOnlyList<Review>> GetReviewsAsync(string professionalId) => reviews.GetForProfessionalAsync(professionalId);

    public InboxThread? ThreadWith(string professionalId) => Threads.FirstOrDefault(t => t.Pro.Id == professionalId);

    public Review? MyReviewOf(string professionalId) => MyReviews.FirstOrDefault(r => r.ProfessionalId == professionalId);

    /// <summary>
    /// The ready-made text to a pro, in the Seeker's language: the job they searched (if this
    /// pro does it, else the pro's trade), their town, and how soon they need it.
    /// </summary>
    public string DefaultMessage(ProfessionalListing pro)
    {
        var serviceId = JobFor(pro);
        var trade = ReferenceData.Find(ReferenceData.Categories, pro.CategoryIds.FirstOrDefault());
        // "Others" says what they typed, or the pro's trade: "Others" alone means nothing in a message.
        var job = !ReferenceData.IsOtherJob(serviceId) && ReferenceData.FindService(serviceId) is { } service ? Label(service)
            : ReferenceData.IsOtherJob(serviceId) && Answers.OtherJobText?.Trim() is { Length: > 0 } typed ? typed
            : Label(trade);
        var town = ReferenceData.Find(ReferenceData.Cities, Answers.CityId ?? pro.CityIds.FirstOrDefault());
        var template = Answers.UrgencyId switch
        {
            ReferenceData.UrgencyToday => Copy.MessageToday,
            ReferenceData.UrgencyThisWeek => Copy.MessageThisWeek,
            _ => Copy.MessageCanvass,
        };
        return text.Format(template, job, Label(town));
    }

    public string ChatIdWith(string professionalId) => Chat.IdFor(professionalId, MyId);

    /// <summary>The chat with a pro, oldest message first (empty before the first message).</summary>
    public Task<IReadOnlyList<ChatMessage>> GetMessagesAsync(string professionalId) => chats.GetMessagesAsync(ChatIdWith(professionalId));

    /// <summary>
    /// Calls <paramref name="changed"/> with the chat when it gets a message after <paramref name="lastMessageId"/>, until disposed.
    /// Pass the chat on to <see cref="ChatChanged"/>.
    /// </summary>
    public IDisposable WatchChat(string professionalId, string? lastMessageId, Action<Chat> changed) =>
        chats.Watch(ChatIdWith(professionalId), lastMessageId, changed);

    /// <summary>A watched chat got a message: its Inbox row shows it, without re-reading the Inbox.</summary>
    public void ChatChanged(Chat chat)
    {
        if (ThreadWith(chat.ProfessionalId) is { } thread)
        {
            Put(thread.Pro, chat);
        }
    }

    /// <summary>
    /// Sends a message to the pro; the first one starts the chat, with the job from the Seeker's answers.
    /// Returns the message as the server saved it, for the page to show.
    /// </summary>
    public Task<ChatMessage> SendAsync(ProfessionalListing pro, string message) => PostAsync(pro, ChatMessageKind.Text, message.Trim());

    /// <summary>Opens the dialer and logs the call in the chat: the logged message, or null if the dialer couldn't open.</summary>
    public async Task<ChatMessage?> CallAsync(ProfessionalListing pro)
    {
        if (!await phone.CallAsync(pro.MobileNumber))
        {
            return null;
        }

        return await PostAsync(pro, ChatMessageKind.Call, null);
    }

    /// <summary>The Seeker opened the chat: the pro's messages are read.</summary>
    public async Task MarkReadAsync(string professionalId)
    {
        if (ThreadWith(professionalId) is { } thread && thread.Chat.IsUnreadFor(ChatSide.Seeker))
        {
            await chats.MarkReadAsync(thread.Chat.Id, ChatSide.Seeker);
            Put(thread.Pro, thread.Chat with { SeekerReadAt = DateTimeOffset.Now });
        }
    }

    /// <summary>
    /// Saves the Seeker's review of a pro they contacted (replacing an earlier one), and
    /// remembers the name they put on it. Pros they never contacted can't be reviewed.
    /// </summary>
    public async Task<bool> SubmitReviewAsync(ProfessionalListing pro, int rating, string? comment, string reviewerName)
    {
        if (ThreadWith(pro.Id) is not { } thread || rating is < 1 or > 5)
        {
            return false;
        }

        var name = reviewerName.Trim();
        var review = new Review(
            Chat.IdFor(pro.Id, MyId),
            pro.Id,
            MyId,
            name,
            rating,
            string.IsNullOrWhiteSpace(comment) ? null : comment.Trim(),
            thread.Chat.ServiceId,
            DateTimeOffset.Now);
        await reviews.SaveAsync(review);
        MyReviews = [review, .. MyReviews.Where(r => r.ProfessionalId != pro.Id)];

        Answers.ReviewerName = name;
        await SaveAsync();
        return true;
    }

    public Task SetNameAsync(string? name)
    {
        Answers.ReviewerName = string.IsNullOrWhiteSpace(name) ? null : name.Trim();
        return SaveAsync();
    }

    /// <summary>
    /// Lets the Seeker pick their profile photo. There is only ever one: the new photo replaces the old one,
    /// which is then deleted from the photo store.
    /// </summary>
    public async Task<PhotoAddResult> ChangeProfilePhotoAsync()
    {
        var pick = await picker.PickAsync(1);
        if (pick.Status != PhotoPickStatus.Picked)
        {
            return PhotoAddResult.From(pick.Status);
        }

        string id;
        try
        {
            id = await photos.SaveAsync(pick.Photos[0]);
        }
        catch (PhotoStoreException ex)
        {
            return PhotoAddResult.From(ex);
        }

        var oldId = Answers.ProfilePhotoId;
        Answers.ProfilePhotoId = id;
        ProfilePhotoUrl = await photos.GetUrlAsync(id);
        await SaveAsync();
        if (oldId is not null && oldId != id)
        {
            await photos.DeleteAsync(oldId);
        }

        return new PhotoAddResult(1, 0);
    }

    /// <summary>Drops what's held in memory after Start over, so the next load reads fresh.</summary>
    public void Forget()
    {
        _state = null;
        ProfilePhotoUrl = null;
        _loading = null;
        _refreshedAt = DateTimeOffset.MinValue;
        MyId = LocalSeekerId;
        Threads = [];
        MyReviews = [];
    }

    /// <summary>Re-reads the Inbox and reviews unless they were read within <see cref="ActivityFreshFor"/> (a tab opening).</summary>
    public Task RefreshIfStaleAsync() =>
        DateTimeOffset.UtcNow - _refreshedAt < ActivityFreshFor ? Task.CompletedTask : RefreshActivityAsync();

    /// <summary>Re-reads the Inbox and the Seeker's reviews, side by side; the pros' listings come from the cache when it has them.</summary>
    public async Task RefreshActivityAsync()
    {
        var inbox = chats.GetInboxAsync(MyId, ChatSide.Seeker);
        var written = reviews.GetBySeekerAsync(MyId);
        var chatList = await inbox;
        var pros = await Task.WhenAll(chatList.Select(chat => profiles.GetByIdAsync(chat.ProfessionalId)));

        // A pro who took their listing down drops out of the Inbox.
        Threads = chatList.Zip(pros, (chat, pro) => pro is null ? null : new InboxThread(pro, chat)).OfType<InboxThread>().ToList();
        MyReviews = await written;
        _refreshedAt = DateTimeOffset.UtcNow;
        Changed?.Invoke();
    }

    private async Task<bool> LoadAsync()
    {
        _state = await store.LoadAsync();
        if (_state.Role != UserRole.Seeker || _state.CompletedAt is null)
        {
            return false;
        }

        var user = await identity.GetCurrentUserAsync();
        MyId = user?.Uid ?? LocalSeekerId;

        // Reviews show "Maricel D.", from the account's name, until they change it on the Me page.
        Answers.ReviewerName ??= PersonName.Short(user?.DisplayName);
        ProfilePhotoUrl = Answers.ProfilePhotoId is { } photoId ? await photos.GetUrlAsync(photoId) : null;

        await RefreshActivityAsync();
        return true;
    }

    private async Task<ChatMessage> PostAsync(ProfessionalListing pro, ChatMessageKind kind, string? message)
    {
        var chatId = ChatIdWith(pro.Id);
        var sent = new ChatMessage(Guid.NewGuid().ToString("N"), chatId, MyId, kind, message, DateTimeOffset.Now);

        // Only used when this starts the chat: the job comes from the Seeker's answers.
        var chat = new Chat(chatId, pro.Id, MyId, pro.DisplayName, Answers.ReviewerName ?? text.Get(Copy.ChatCustomer),
            JobFor(pro), Answers.CityId, Answers.UrgencyId, Answers.BudgetId, sent, sent.At, null, sent.At);
        var saved = await chats.SendAsync(chat, sent);
        Put(pro, saved);
        return saved.LastMessage;
    }

    /// <summary>Puts the chat at the top of the Inbox (replacing its old row), as the server just answered.</summary>
    private void Put(ProfessionalListing pro, Chat chat)
    {
        Threads = [new InboxThread(pro, chat), .. Threads.Where(t => t.Chat.Id != chat.Id)];
        Changed?.Invoke();
    }

    /// <summary>The searched job, if this pro does it.</summary>
    private string? JobFor(ProfessionalListing pro) =>
        Answers.ServiceId is { } serviceId && pro.ServiceIds.Contains(serviceId) ? serviceId : null;

    private async Task SaveAsync()
    {
        await store.SaveAsync(_state!);
        Changed?.Invoke();
    }

    private string Label(CatalogOption? option) => option is null ? string.Empty : text.Get(option.Label);
}
