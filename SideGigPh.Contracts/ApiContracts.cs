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
    public const string Android = "android";
    public const string Ios = "ios";
}

/// <summary>The error codes the API answers with, as <c>{ "error": "…" }</c>.</summary>
public static class ApiErrors
{
    /// <summary>409 on publish: anti-fraud rule 1, the number already has a listing.</summary>
    public const string PhoneTaken = "phone-taken";

    /// <summary>409 on publish: anti-fraud rule 1, another account uses the number as a Job Seeker.</summary>
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

/// <summary>A stored photo: its SHA-256 id and public URL. Also the answer to <c>POST photos</c>.</summary>
public sealed record PhotoDto(string Id, string Url);

/// <summary>
/// A published Skilled Worker listing (<c>GET professionals</c>, <c>GET professionals/{id}</c>), with its stars
/// (null with no reviews yet), so search ranks in one request, and how many references vouch for the worker
/// (only the count: reference names and numbers never leave the server).
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
    string? RateTierId,
    PhotoDto? ProfilePhoto,
    IReadOnlyList<PhotoDto> WorkPhotos,
    ReviewSummaryDto? Reviews = null,
    int ReferenceCount = 0);

/// <summary>
/// <c>PUT professionals/me</c>. The listing id is the caller's uid and its number is the account's verified
/// number, both taken from the sign-in token, never from here. Work photos are newest first. Publishing needs a
/// profile photo, at least 2 work photos and at least 1 reference; references are private (never in <see cref="ListingDto"/>).
/// </summary>
public sealed record PublishListingRequest(
    string DisplayName,
    string CategoryId,
    string AreaId,
    IReadOnlyList<string> ServiceIds,
    IReadOnlyList<string> CityIds,
    string? ExperienceId,
    string? RateTierId,
    string? ProfilePhotoId,
    IReadOnlyList<string> WorkPhotoIds,
    IReadOnlyList<ReferenceDto>? References = null);

/// <summary>Someone who can vouch for a Skilled Worker: a name and an E.164 Philippine mobile number.</summary>
public sealed record ReferenceDto(string Name, string MobileNumber);

public sealed record ReviewDto(
    string ProfessionalId,
    string SeekerId,
    string ReviewerName,
    int Rating,
    string? Comment,
    string? ServiceId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary><c>PUT reviews</c>: the caller's review of a Skilled Worker, replacing their earlier one.</summary>
/// <summary>When a Seeker may rate a Skilled Worker. The app hides the Rate buttons until then, and the server refuses earlier reviews.</summary>
public static class ReviewRules
{
    /// <summary>A Skilled Worker can be rated only this long after the Seeker's first message, so stars are about the job, not the first reply.</summary>
    public static readonly TimeSpan WaitAfterChatStarts = TimeSpan.FromHours(24);

    /// <summary>True once <see cref="WaitAfterChatStarts"/> has passed since the chat started.</summary>
    public static bool CanRate(DateTimeOffset chatStartedAt, DateTimeOffset now) => now - chatStartedAt >= WaitAfterChatStarts;
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
    DateTimeOffset CreatedAt);

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
}

/// <summary>
/// The app's config (<c>GET config</c>): every answer list that is fine to be a few hours stale, loaded once when the
/// app opens and kept on the phone for <c>ConfigFor</c>. All lists are in display order and leave out retired rows.
/// <paramref name="Catalog"/>: the trades, each with its jobs most-requested first and "Others" last.
/// <paramref name="PriceTiers"/>: cheapest first (the Seeker's budget and the worker's rate are compared by position).
/// </summary>
public sealed record ConfigDto(
    List<CategoryDto> Catalog,
    List<OptionDto> Towns,
    List<OptionDto> PriceTiers,
    List<OptionDto> Experience,
    List<OptionDto> Urgencies,
    List<OptionDto> IncomeGoals);

/// <summary>One answer in a config list: <c>taytay</c> "Taytay", <c>500-1000</c> "₱500 – ₱1,000" (Amount 750)…</summary>
/// <param name="Amount">Pesos for price tiers (a typical job) and income goals (per month); 0 otherwise.</param>
public sealed record OptionDto(string Id, string English, string Taglish, string? Icon, int Amount);

/// <summary>A trade: <c>aircon</c>, "Aircon Technician", ❄️, with its jobs.</summary>
public sealed record CategoryDto(
    string Id,
    string English,
    string Taglish,
    string? Icon,
    string? HintEnglish,
    string? HintTaglish,
    List<ServiceDto> Services);

/// <summary>A job within a trade: <c>aircon-cleaning</c>, "Aircon cleaning" / "Pa-linis ng aircon".</summary>
public sealed record ServiceDto(string Id, string English, string Taglish, string? Icon);

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
}

/// <summary>One worker in ranked search results, with why they ranked where they did.</summary>
public sealed record SearchHitDto(ListingDto Listing, bool OffersService, bool ServesTown, bool FitsBudget);

/// <summary>
/// <c>GET search/professionals</c>: one page of results, ranked on the server. <paramref name="Hits"/> run in ranked
/// order across both groups, in-town first: the first <paramref name="InTownCount"/> results of the whole list go to the
/// Seeker's town, the next <paramref name="OtherTownsCount"/> come from other towns nearby.
/// <paramref name="ServiceFallback"/>: always false now (the job is a hard filter); kept for app versions that read it.
/// </summary>
public sealed record SearchPageDto(List<SearchHitDto> Hits, int InTownCount, int OtherTownsCount, bool ServiceFallback);
