namespace SideGigPh.Contracts;

// What goes over the wire between the app and SideGigPh.Api. Ids are the app's ReferenceData ids
// (categories, services, towns, price tiers…) and Firebase uids. Times are UTC.

/// <summary><c>english</c> / <c>taglish</c>, <c>seeker</c> / <c>professional</c>, <c>text</c> / <c>call</c>, <c>android</c> / <c>ios</c>.</summary>
public static class Wire
{
    public const string English = "english";
    public const string Taglish = "taglish";
    public const string Seeker = "seeker";
    public const string Professional = "professional";
    public const string Text = "text";
    public const string Call = "call";

    /// <summary>A chat message saying the household sent the picked worker its exact place (no coordinates in it).</summary>
    public const string Pin = "pin";
    public const string Android = "android";
    public const string Ios = "ios";
}

/// <summary>The error codes the API answers with, as <c>{ "error": "…" }</c>.</summary>
/// <summary>
/// The headers that say which builds are talking: the app names its build on every call, the API names its own on
/// every answer. Error reports and the server's error logs carry both.
/// </summary>
public static class ApiHeaders
{
    /// <summary>On every request: the app build, e.g. <c>0.1.0-rc.12 (beta; 48466c9)</c>.</summary>
    public const string AppVersion = "X-App-Version";

    /// <summary>On every answer: the API build, e.g. <c>0.1.0-rc.57+48466c9</c>.</summary>
    public const string ApiVersion = "X-Api-Version";
}

public static class ApiErrors
{
    /// <summary>409 on publish: anti-fraud rule 1, the number already has a listing.</summary>
    public const string PhoneTaken = "phone-taken";

    /// <summary>Retired: no longer sent (the account holding the verified number now wins). Kept so the name is never reused.</summary>
    public const string PhoneUsedBySeeker = "phone-used-by-seeker";

    /// <summary>409 on upload or publish: anti-fraud rule 2, another account has that photo.</summary>
    public const string PhotoInUse = "photo-in-use";

    /// <summary>400 on any write: the request body is missing or isn't the expected JSON.</summary>
    public const string BadBody = "bad-body";

    /// <summary>400 on publish: this listing field is missing or not a valid id, e.g. <c>invalid-categoryId</c>.</summary>
    public static string Invalid(string field) => $"invalid-{field}";

    /// <summary>403 on publish: the account has no verified phone number yet.</summary>
    public const string PhoneRequired = "phone-required";

    /// <summary>403 on review: only a Seeker who chatted with the Skilled Worker can review them.</summary>
    public const string NoChat = "no-chat";

    /// <summary>403 on review: the chat started less than <see cref="ReviewRules.WaitAfterChatStarts"/> ago.</summary>
    public const string ReviewTooSoon = "review-too-soon";

    /// <summary>
    /// 403 on a review or a worker's rating of a household: it is about a completed job and both sides' reviews already
    /// show (<see cref="ReviewRules.IsLocked"/>), so it can't be changed any more.
    /// </summary>
    public const string ReviewLocked = "review-locked";

    /// <summary>429 on a chat message, new chat, "Others" note, photo upload or review: this account sent too many too fast.</summary>
    public const string TooManyRequests = "too-many-requests";

    /// <summary>403 on a chat message: one of the two blocked the other.</summary>
    public const string Blocked = "blocked";

    /// <summary>400 on a verification upload: the consent box wasn't ticked (<c>consent=true</c>).</summary>
    public const string ConsentRequired = "consent-required";

    /// <summary>409 on a verification upload: this badge is already waiting for the operator, or already approved.</summary>
    public const string AlreadySent = "already-sent";

    /// <summary>403 on <c>PUT household-reviews</c>: no completed job (picked, both marked done) between the two.</summary>
    public const string NoCompletedJob = "no-completed-job";

    /// <summary>409 on <c>POST picked-jobs/{postId}/cancel</c>: both sides already marked the job done, so it can't be called off.</summary>
    public const string JobCompleted = "job-completed";

    /// <summary>403 on an <c>ops/…</c> route: the caller isn't an operator.</summary>
    public const string NotAdmin = "not-admin";

    /// <summary>409 on <c>DELETE ops/catalog/services/{id}</c>: a trade's "Others" job can be renamed, never retired.</summary>
    public const string OthersStays = "others-stays";

    /// <summary>409 on <c>POST ops/catalog/services/{id}/restore</c>: the job has no price guide; give it one first (the app refuses a job without).</summary>
    public const string NeedsPrice = "needs-price";

    /// <summary>415 on <c>POST photos/selfie</c>: the selfie isn't a JPEG (the phone sends one, already made small).</summary>
    public const string SelfieNotJpeg = "selfie-not-jpeg";

    /// <summary>413 on <c>POST photos/selfie</c>: over 2 MB, or a side over 1280 pixels (the server doesn't resize selfies).</summary>
    public const string SelfieTooBig = "selfie-too-big";

    /// <summary>400 on <c>POST photos/selfie</c>: starts like a JPEG but is cut short or doesn't decode.</summary>
    public const string SelfieBroken = "selfie-broken";

    /// <summary>500: the function threw; <see cref="ApiError.Detail"/> has the exception and stack trace.</summary>
    public const string Exception = "exception";
}

/// <summary>
/// An error answer. <paramref name="Detail"/> is for debugging (the exception with its stack trace, or what was
/// wrong with the request) while <c>ApiResults.ShowDetails</c> is on; the app logs it and never shows it to users.
/// </summary>
public sealed record ApiError(string Error, string? Detail = null);

/// <summary>The signed-in person (<c>GET/PUT users/me</c>). <paramref name="ProfilePhotoId"/> is the account's one profile photo (both sides).</summary>
public sealed record UserDto(
    string Uid,
    string? DisplayName,
    string? PhoneNumber,
    string? Email,
    string Language,
    string? Role,
    string? IncomeGoalId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string? ProfilePhotoId = null);

/// <summary>A stored photo: its random id and public URL. The answer to <c>POST photos</c> and <c>POST photos/selfie</c>.</summary>
public sealed record PhotoDto(string Id, string Url);

/// <summary>
/// A published Skilled Worker listing (<c>GET professionals</c>, <c>GET professionals/{id}</c>), with its stars
/// (null with no reviews yet), so search ranks in one request. <c>RatePesos</c>: the worker's average price per
/// service (<see cref="WorkerPrice"/>).
/// </summary>
public sealed record ListingDto(
    string Id,
    string DisplayName,
    string MobileNumber,
    string CategoryId,
    string AreaId,
    IReadOnlyList<string> ServiceIds,
    IReadOnlyList<string> CityIds,
    string? ExperienceId,
    int RatePesos,
    PhotoDto? ProfilePhoto,
    IReadOnlyList<PhotoDto> WorkPhotos,
    ReviewSummaryDto? Reviews = null,
    IReadOnlyList<BadgeDto>? Badges = null,
    string? OtherWork = null,
    int CompletedJobs = 0);

/// <summary>
/// <c>PUT professionals/me</c>. The listing id is the caller's uid and its number is the account's verified
/// number, both taken from the sign-in token, never from here. Work photos are newest first. Publishing needs a
/// profile photo (work photos are optional, 0–6). An older app may still send a <c>references</c> list: it is ignored.
/// <paramref name="Latitude"/> / <paramref name="Longitude"/> are the worker's base point and <paramref name="RadiusKm"/>
/// how far they travel (1–50): all optional and private (never in <see cref="ListingDto"/>). Left out, the server keeps
/// what it has, or starts from the middle of the listing's towns and 10 km.
/// </summary>
public sealed record PublishListingRequest(
    string DisplayName,
    string CategoryId,
    string AreaId,
    IReadOnlyList<string> ServiceIds,
    IReadOnlyList<string> CityIds,
    string? ExperienceId,
    int? RatePesos,
    string? ProfilePhotoId,
    IReadOnlyList<string> WorkPhotoIds,
    double? Latitude = null,
    double? Longitude = null,
    int? RadiusKm = null,
    string? OtherWork = null);

/// <summary>A Seeker's review of a Skilled Worker.</summary>
/// <param name="VerifiedJob">The Seeker picked this worker for a job both marked done.</param>
/// <param name="Hidden">A blind review only its writer sees for now (<see cref="ReviewRules.IsHidden"/>): it shows when the worker reviews back or <see cref="ReviewRules.BlindFor"/> passes.</param>
/// <param name="Locked">For its writer only: it can't be changed any more (<see cref="ReviewRules.IsLocked"/>).</param>
public sealed record ReviewDto(
    string ProfessionalId,
    string SeekerId,
    string ReviewerName,
    int Rating,
    string? Comment,
    string? ServiceId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    bool VerifiedJob = false,
    bool Hidden = false,
    bool Locked = false);

/// <summary><c>PUT reviews</c>: the caller's review of a Skilled Worker, replacing their earlier one.</summary>
/// <summary>When a Seeker may rate a Skilled Worker. The app hides the Rate buttons until then, and the server refuses earlier reviews.</summary>
public static class ReviewRules
{
    /// <summary>A Skilled Worker can be rated only this long after the Seeker's first message, so stars are about the job, not the first reply.</summary>
    public static readonly TimeSpan WaitAfterChatStarts = TimeSpan.FromHours(24);

    /// <summary>True once <see cref="WaitAfterChatStarts"/> has passed since the chat started.</summary>
    public static bool CanRate(DateTimeOffset chatStartedAt, DateTimeOffset now) => now - chatStartedAt >= WaitAfterChatStarts;

    /// <summary>
    /// Blind reviews: after a completed job (picked, both marked done) each side's review of the other stays hidden
    /// until the other side has reviewed too, or until this long after the job was completed. So nobody rates in
    /// fear of the other's answer (no retaliation, no inflated stars). Only the writer sees a hidden review.
    /// </summary>
    public static readonly TimeSpan BlindFor = TimeSpan.FromDays(14);

    /// <summary>When the reviews of a job completed at <paramref name="completedAt"/> show, whoever reviewed.</summary>
    public static DateTimeOffset OpensAt(DateTimeOffset completedAt) => completedAt + BlindFor;

    /// <summary>
    /// True when a review (last written at <paramref name="writtenAt"/>) is still hidden from everyone but its writer:
    /// it was written after one of the pair's completed jobs (<paramref name="jobsCompletedAt"/>), that job's
    /// <see cref="BlindFor"/> hasn't passed, and the other side hasn't reviewed since that job
    /// (<paramref name="otherWrittenAt"/>, null when they never did). A review written before the job stays as it was.
    /// The server's SQL (<c>BlindReviewFilter</c>) is the same rule.
    /// </summary>
    public static bool IsHidden(DateTimeOffset writtenAt, DateTimeOffset? otherWrittenAt, IEnumerable<DateTimeOffset> jobsCompletedAt, DateTimeOffset now) =>
        jobsCompletedAt.Any(done => now < OpensAt(done) && writtenAt >= done && !(otherWrittenAt >= done));

    /// <summary>
    /// True when a review (last written at <paramref name="writtenAt"/>) can't be changed or deleted any more: it is
    /// about the pair's latest completed job (written since it) and the other side's review of that job shows too
    /// (they reviewed since the job, or <see cref="BlindFor"/> passed). Otherwise one side could lower their stars after
    /// seeing the other's. A hidden review can still be changed, and a newer completed job opens it again. Reviews
    /// with no completed job (one-way, after a chat) never lock.
    /// </summary>
    public static bool IsLocked(DateTimeOffset writtenAt, DateTimeOffset? otherWrittenAt, IEnumerable<DateTimeOffset> jobsCompletedAt, DateTimeOffset now)
    {
        var jobs = jobsCompletedAt.ToList();
        if (jobs.Count == 0)
        {
            return false;
        }

        var latest = jobs.Max();
        return writtenAt >= latest && (otherWrittenAt >= latest || now >= OpensAt(latest));
    }
}

public sealed record SaveReviewRequest(string ProfessionalId, string ReviewerName, int Rating, string? Comment, string? ServiceId);

/// <summary>A worker's average stars and how many reviews, on their <see cref="ListingDto"/>.</summary>
public sealed record ReviewSummaryDto(double Average, int Count);

public sealed record MessageDto(string Id, string ChatId, string SenderId, string Kind, string? Text, DateTimeOffset At);

public sealed record ChatDto(
    string Id,
    string ProfessionalId,
    string SeekerId,
    string ProfessionalName,
    string SeekerName,
    string? ServiceId,
    string? CityId,
    string? UrgencyId,
    string? BudgetId,
    MessageDto LastMessage,
    DateTimeOffset? SeekerReadAt,
    DateTimeOffset? ProfessionalReadAt,
    DateTimeOffset CreatedAt,
    CatalogNameDto? ServiceName = null);

/// <summary>
/// <c>POST chats/{chatId}/messages</c>. The first message creates the chat from the names and job here (only the
/// Seeker can start one); later messages ignore them. The server stamps the time.
/// </summary>
public sealed record SendMessageRequest(
    string MessageId,
    string ProfessionalId,
    string SeekerId,
    string Kind,
    string? Text,
    string ProfessionalName,
    string SeekerName,
    string? ServiceId,
    string? CityId,
    string? UrgencyId,
    string? BudgetId);

/// <summary><c>PUT users/me/devices</c>: this phone gets the caller's new-message notifications (<see cref="Wire.Android"/> / <see cref="Wire.Ios"/>).</summary>
public sealed record SaveDeviceRequest(string Token, string Platform);

/// <summary>
/// What a new-message notification carries, read by the app when it arrives or is tapped, and the Android
/// notification channels it lands on. The server and the app must agree on these, so they live here.
/// </summary>
public static class PushData
{
    /// <summary>The chat the message is in.</summary>
    public const string ChatId = "chatId";

    /// <summary>The Skilled Worker in that chat (the Seeker's chat page is keyed by them).</summary>
    public const string ProfessionalId = "professionalId";

    /// <summary>Which side the receiver is on: <see cref="Wire.Seeker"/> or <see cref="Wire.Professional"/>.</summary>
    public const string Side = "side";

    /// <summary>Android channel for a customer writing to a Skilled Worker: a normal notification (sound, pop-up).</summary>
    public const string CustomersChannel = "customers";

    /// <summary>Android channel for a Skilled Worker answering a Seeker: silent, only the tray line and the icon dot.</summary>
    public const string RepliesChannel = "replies";

    /// <summary>The job post a notification is about (a new post for a worker, a new offer for the household).</summary>
    public const string PostId = "postId";

    /// <summary>Android channel for a new post near a worker: a normal notification (sound, pop-up).</summary>
    public const string RequestsChannel = "requests";

    /// <summary>Android channel for a new offer on a household's post: a normal notification (sound, pop-up).</summary>
    public const string OffersChannel = "offers";
}

/// <summary>
/// The app's config (<c>GET config</c>): every answer list that is fine to be a few hours stale, loaded once when the
/// app opens and kept on the phone for <c>ConfigFor</c>. All lists are in display order and leave out retired rows.
/// <paramref name="Catalog"/>: the trades, each with its jobs most-requested first and "Others" last.
/// <paramref name="DefaultTownId"/>: the launch's town, pre-selected wherever a person picks a town (a <paramref name="Towns"/>
/// id; null from a server before it, or when none is set). <paramref name="Barangays"/>: the barangays we serve, each in
/// one of <paramref name="Towns"/>, in display order (null from a server before them). Both are only defaults and
/// choices: nobody has to pick a barangay.
/// </summary>
public sealed record ConfigDto(
    List<CategoryDto> Catalog,
    List<OptionDto> Towns,
    List<OptionDto> Experience,
    List<OptionDto> Urgencies,
    List<OptionDto> IncomeGoals,
    string? DefaultTownId = null,
    List<BarangayDto>? Barangays = null);

/// <summary>A barangay we serve: <c>angono-san-isidro</c> "San Isidro" in the town <c>angono</c> (the app adds "Brgy.").</summary>
public sealed record BarangayDto(string Id, string TownId, string English, string Taglish);

/// <summary>One answer in a config list: <c>taytay</c> "Taytay", <c>3-10</c> "3–10 years"…</summary>
/// <param name="Amount">Pesos for income goals (per month); 0 otherwise.</param>
public sealed record OptionDto(string Id, string English, string Taglish, string? Icon, int Amount);

/// <summary>
/// A trade's or job's name as it was when a post or chat was made (<c>JobPostDto.ServiceName</c>, <c>ChatDto.ServiceName</c>,
/// <c>PickedJobDto.ServiceName</c>), like an order line keeping a product's title: an operator renaming the trade or
/// job later (<c>ops/catalog</c>) changes the config, never this. Null when the post or chat has none ("Others").
/// </summary>
public sealed record CatalogNameDto(string English, string Taglish);

/// <summary>A trade: <c>aircon</c>, "Aircon Technician", ❄️, with its jobs.</summary>
public sealed record CategoryDto(
    string Id,
    string English,
    string Taglish,
    string? Icon,
    string? HintEnglish,
    string? HintTaglish,
    List<ServiceDto> Services);

/// <summary>
/// A job within a trade: <c>aircon-cleaning</c>, "Aircon cleaning" / "Pa-linis ng aircon", with what it usually costs
/// (<paramref name="Price"/>; null from a server before price guides).
/// </summary>
public sealed record ServiceDto(string Id, string English, string Taglish, string? Icon, PriceGuideDto? Price = null);

/// <summary>
/// What a job usually costs, in pesos (labor only): the low, average and high prices people pay. Only a guide: workers
/// set their own price (<see cref="WorkerPrice"/>) and Seekers their own budget.
/// </summary>
public sealed record PriceGuideDto(int Min, int Average, int High);

/// <summary>
/// A Skilled Worker's price (<c>ListingDto.RatePesos</c>): their average price per service, in pesos. Only a guide
/// for customers: the final price is what the two agree on before the work.
/// </summary>
public static class WorkerPrice
{
    /// <summary>The highest price a worker may save.</summary>
    public const int MaxPesos = 1_000_000;

    /// <summary>A price a worker may save: more than ₱0, at most <see cref="MaxPesos"/>.</summary>
    public static bool IsValid(int? pesos) => pesos is > 0 and <= MaxPesos;
}

/// <summary>How a Seeker orders search results (<c>sort</c> on <c>GET search/professionals</c>).</summary>
public static class SearchSort
{
    /// <summary>The ranked score (the default).</summary>
    public const string BestMatch = "best";

    /// <summary>Cheapest worker first, the score breaking ties.</summary>
    public const string LowestPrice = "price";
}

/// <summary>
/// "Others": the tile after the trades (<see cref="TradeId"/>, saved as both the trade and the job) and each trade's
/// last job (<c>…-other</c>). The person types what they mean; workers' and households' texts are matched by shared
/// words (search, <c>SearchRanker</c>).
/// </summary>
public static class OtherWork
{
    /// <summary>The "Others" trade: not a trade we offer, the person typed the kind of work.</summary>
    public const string TradeId = "other";

    /// <summary>Most characters of a typed "Others" text.</summary>
    public const int MaxText = 100;

    private const string JobSuffix = "-other";

    /// <summary>The "Others" job of a trade, or the "Others" trade itself.</summary>
    public static bool IsOther(string? id) => id == TradeId || id?.EndsWith(JobSuffix, StringComparison.Ordinal) == true;
}

/// <summary>
/// A Seeker's budget as it travels (<c>budgetId</c> on search, <c>ChatDto.BudgetId</c>): a peso range
/// <c>"800-1500"</c>, open-ended <c>"1500-up"</c>, or "up to" <c>"under-500"</c> (a post's budget with no "from").
/// Anything else (<c>unsure</c>) is no budget.
/// </summary>
public static class BudgetRange
{
    private const string Up = "-up";
    private const string Under = "under-";

    /// <summary><c>"800-1500"</c>, <c>"800-up"</c> with no top, or <c>"under-1500"</c> with no bottom (0).</summary>
    public static string Format(int min, int? max) => max is { } top ? min > 0 ? $"{min}-{top}" : $"{Under}{top}" : $"{min}{Up}";

    /// <summary>Reads a budget id; false for "not sure", an empty or a malformed one.</summary>
    public static bool TryParse(string? id, out int min, out int? max)
    {
        min = 0;
        max = null;
        if (string.IsNullOrEmpty(id))
        {
            return false;
        }

        if (id.StartsWith(Under, StringComparison.Ordinal))
        {
            if (!TryAmount(id[Under.Length..], out var top))
            {
                return false;
            }

            max = top;
            return true;
        }

        if (id.EndsWith(Up, StringComparison.Ordinal))
        {
            return TryAmount(id[..^Up.Length], out min);
        }

        var dash = id.IndexOf('-');
        if (dash <= 0 || !TryAmount(id[..dash], out min) || !TryAmount(id[(dash + 1)..], out var upper) || upper < min)
        {
            min = 0;
            return false;
        }

        max = upper;
        return true;
    }

    private static bool TryAmount(string text, out int amount) =>
        int.TryParse(text, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out amount) && amount > 0;
}

/// <summary>
/// What someone typed after picking "Others" in a trade (<c>POST other-jobs</c>), kept for analytics only:
/// it never becomes a job and is never shown. <paramref name="Side"/> is <see cref="Wire.Seeker"/> or <see cref="Wire.Professional"/>.
/// </summary>
public sealed record OtherJobRequest(string CategoryId, string Side, string Text);

/// <summary>Page sizes the app asks for; the server caps what it gives (<c>ChatStore.MaxPage</c>, <c>ProfessionalStore.MaxSearchPage</c>).</summary>
public static class Paging
{
    /// <summary>Chat messages per page: a chat opens on its newest 20 and loads 20 older each time the person scrolls to the top.</summary>
    public const int Messages = 20;

    /// <summary>Search results per page: 4 at first, 4 more each time the Seeker scrolls to the end.</summary>
    public const int Search = 4;

    /// <summary>Inbox chats per page: the newest 30, and 30 older each time the person scrolls to the end.</summary>
    public const int Inbox = 30;

    /// <summary>Feedback board suggestions per page: the 20 most voted, and 20 more each time the person scrolls to the end.</summary>
    public const int Feedback = 20;

    /// <summary>Job posts per page of a worker's feed: the 10 most relevant, and 10 more each time they scroll to the end.</summary>
    public const int JobPosts = 10;
}

/// <summary>
/// One worker in ranked search results, with why they ranked where they did. <paramref name="FitsBudget"/> /
/// <paramref name="AboveBudget"/>: their price is within / over the top of the Seeker's budget (both false with no
/// budget). Being over never hides a worker, it only lowers their score.
/// </summary>
public sealed record SearchHitDto(ListingDto Listing, bool OffersService, bool ServesTown, bool FitsBudget, bool AboveBudget);

/// <summary>
/// <c>GET search/professionals</c>: one page of results, ranked on the server. <paramref name="Hits"/> run in ranked
/// order across both groups, in-town first: the first <paramref name="InTownCount"/> results of the whole list go to the
/// Seeker's town, the next <paramref name="OtherTownsCount"/> come from other towns nearby.
/// <paramref name="ServiceFallback"/>: always false now (the job is a hard filter); kept for app versions that read it.
/// </summary>
public sealed record SearchPageDto(List<SearchHitDto> Hits, int InTownCount, int OtherTownsCount, bool ServiceFallback);

/// <summary>What a report is about (<see cref="ReportRequest.Kind"/>).</summary>
public static class ReportKinds
{
    public const string Listing = "listing";
    public const string Review = "review";
    public const string Chat = "chat";
    public const string User = "user";

    public static readonly IReadOnlyList<string> All = [Listing, Review, Chat, User];
}

/// <summary>Why someone reports (<see cref="ReportRequest.Reason"/>); the app shows one tile per reason.</summary>
public static class ReportReasons
{
    public const string Scam = "scam";
    public const string Harassment = "harassment";
    public const string Fake = "fake";
    public const string Inappropriate = "inappropriate";
    public const string Employment = "employment";
    public const string Other = "other";

    public static readonly IReadOnlyList<string> All = [Scam, Harassment, Fake, Inappropriate, Employment, Other];
}

/// <summary>
/// <c>POST reports</c>: the caller reports <paramref name="TargetUid"/> (the worker of a listing, a review's writer, the
/// other person in a chat). <paramref name="SubjectId"/> is the chat id for a chat report. With
/// <paramref name="AlsoBlock"/> the caller also blocks them, so they disappear for the caller at once.
/// </summary>
public sealed record ReportRequest(string TargetUid, string Kind, string Reason, string? SubjectId, string? Note, bool AlsoBlock);

