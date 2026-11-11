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
    AppText text)
{
    /// <summary>The Seeker's id when nobody is signed in (no sign-in provider registered). With an account it is their uid: <see cref="MyId"/>.</summary>
    public const string LocalSeekerId = "me";

    private OnboardingState? _state;
    private Task<bool>? _loading;

    /// <summary>The Seeker's id in chats and reviews: the signed-in user's uid (set when the workspace loads), or <see cref="LocalSeekerId"/>.</summary>
    public string MyId { get; private set; } = LocalSeekerId;

    /// <summary>Raised after anything here changes, so the shell can re-render.</summary>
    public event Action? Changed;

    public SeekerAnswers Answers => _state?.Seeker ?? new SeekerAnswers();

    /// <summary>What search results show: the interview answers, as changed on the results page since.</summary>
    public SearchRequest Request => new(Answers.CategoryId, Answers.ServiceId, Answers.CityId, Answers.BudgetId);

    /// <summary>One chat per pro the Seeker contacted, most recent message first.</summary>
    public IReadOnlyList<InboxThread> Threads { get; private set; } = [];

    /// <summary>Chats where the pro wrote last and the Seeker hasn't opened it since: the Inbox badge.</summary>
    public int UnreadCount => Threads.Count(thread => thread.Chat.IsUnreadFor(ChatSide.Seeker));

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
        var job = JobFor(pro) is { } serviceId
            ? ReferenceData.FindService(serviceId)
            : ReferenceData.Find(ReferenceData.Categories, pro.CategoryIds.FirstOrDefault());
        var town = ReferenceData.Find(ReferenceData.Cities, Answers.CityId ?? pro.CityIds.FirstOrDefault());
        var template = Answers.UrgencyId switch
        {
            ReferenceData.UrgencyToday => Copy.MessageToday,
            ReferenceData.UrgencyThisWeek => Copy.MessageThisWeek,
            _ => Copy.MessageCanvass,
        };
        return text.Format(template, Label(job), Label(town));
    }

    public string ChatIdWith(string professionalId) => Chat.IdFor(professionalId, MyId);

    /// <summary>The chat with a pro, oldest message first (empty before the first message).</summary>
    public Task<IReadOnlyList<ChatMessage>> GetMessagesAsync(string professionalId) => chats.GetMessagesAsync(ChatIdWith(professionalId));

    /// <summary>Calls <paramref name="changed"/> when the chat with the pro gets a message, until disposed.</summary>
    public IDisposable WatchChat(string professionalId, Action changed) => chats.Watch(ChatIdWith(professionalId), changed);

    /// <summary>Sends a message to the pro; the first one starts the chat, with the job from the Seeker's answers.</summary>
    public Task SendAsync(ProfessionalListing pro, string message) => PostAsync(pro, ChatMessageKind.Text, message.Trim());

    /// <summary>Opens the dialer and logs the call in the chat. False if the dialer couldn't open.</summary>
    public async Task<bool> CallAsync(ProfessionalListing pro)
    {
        if (!await phone.CallAsync(pro.MobileNumber))
        {
            return false;
        }

        await PostAsync(pro, ChatMessageKind.Call, null);
        return true;
    }

    /// <summary>The Seeker opened the chat: the pro's messages are read.</summary>
    public async Task MarkReadAsync(string professionalId)
    {
        if (ThreadWith(professionalId) is { } thread && thread.Chat.IsUnreadFor(ChatSide.Seeker))
        {
            await chats.MarkReadAsync(thread.Chat.Id, ChatSide.Seeker);
            await RefreshActivityAsync();
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
        await reviews.SaveAsync(new Review(
            Chat.IdFor(pro.Id, MyId),
            pro.Id,
            MyId,
            name,
            rating,
            string.IsNullOrWhiteSpace(comment) ? null : comment.Trim(),
            thread.Chat.ServiceId,
            DateTimeOffset.Now));

        Answers.ReviewerName = name;
        await store.SaveAsync(_state!);
        await RefreshActivityAsync();
        return true;
    }

    public Task SetNameAsync(string? name)
    {
        Answers.ReviewerName = string.IsNullOrWhiteSpace(name) ? null : name.Trim();
        return SaveAsync();
    }

    /// <summary>Drops what's held in memory after Start over, so the next load reads fresh.</summary>
    public void Forget()
    {
        _state = null;
        _loading = null;
        MyId = LocalSeekerId;
        Threads = [];
        MyReviews = [];
    }

    /// <summary>Re-reads the Inbox and the Seeker's reviews.</summary>
    public async Task RefreshActivityAsync()
    {
        var threads = new List<InboxThread>();
        foreach (var chat in await chats.GetInboxAsync(MyId, ChatSide.Seeker))
        {
            // A pro who took their listing down drops out of the Inbox.
            if (await profiles.GetByIdAsync(chat.ProfessionalId) is { } pro)
            {
                threads.Add(new InboxThread(pro, chat));
            }
        }

        Threads = threads;
        MyReviews = await reviews.GetBySeekerAsync(MyId);
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

        await RefreshActivityAsync();
        return true;
    }

    private async Task PostAsync(ProfessionalListing pro, ChatMessageKind kind, string? message)
    {
        var chatId = ChatIdWith(pro.Id);
        var sent = new ChatMessage(Guid.NewGuid().ToString("N"), chatId, MyId, kind, message, DateTimeOffset.Now);

        // Only used when this starts the chat: the job comes from the Seeker's answers.
        var chat = new Chat(chatId, pro.Id, MyId, pro.DisplayName, Answers.ReviewerName ?? text.Get(Copy.ChatCustomer),
            JobFor(pro), Answers.CityId, Answers.UrgencyId, Answers.BudgetId, sent, sent.At, null, sent.At);
        await chats.SendAsync(chat, sent);
        await RefreshActivityAsync();
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
