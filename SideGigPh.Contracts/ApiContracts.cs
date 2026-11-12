namespace SideGigPh.Contracts;

// What goes over the wire between the app and SideGigPh.Api. Ids are the app's ReferenceData ids
// (categories, services, towns, price tiers…) and Firebase uids. Times are UTC.

/// <summary><c>english</c> / <c>taglish</c>, <c>seeker</c> / <c>professional</c>, <c>text</c> / <c>call</c>.</summary>
public static class Wire
{
    public const string English = "english";
    public const string Taglish = "taglish";
    public const string Seeker = "seeker";
    public const string Professional = "professional";
    public const string Text = "text";
    public const string Call = "call";
}

/// <summary>The error codes the API answers with, as <c>{ "error": "…" }</c>.</summary>
public static class ApiErrors
{
    /// <summary>409 on publish: anti-fraud rule 1, the number already has a listing.</summary>
    public const string PhoneTaken = "phone-taken";

    /// <summary>409 on upload or publish: anti-fraud rule 2, another account has that photo.</summary>
    public const string PhotoInUse = "photo-in-use";

    /// <summary>403 on publish: the account has no verified phone number yet.</summary>
    public const string PhoneRequired = "phone-required";

    /// <summary>403 on review: only a Seeker who chatted with the Skilled Worker can review them.</summary>
    public const string NoChat = "no-chat";
}

public sealed record ApiError(string Error);

/// <summary>The signed-in person (<c>GET/PUT users/me</c>).</summary>
public sealed record UserDto(
    string Uid,
    string? DisplayName,
    string? PhoneNumber,
    string? Email,
    string Language,
    string? Role,
    string? IncomeGoalId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>A stored photo: its SHA-256 id and public URL. Also the answer to <c>POST photos</c>.</summary>
public sealed record PhotoDto(string Id, string Url);

/// <summary>A published Skilled Worker listing (<c>GET professionals</c>, <c>GET professionals/{id}</c>).</summary>
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
    IReadOnlyList<PhotoDto> WorkPhotos);

/// <summary>
/// <c>PUT professionals/me</c>. The listing id is the caller's uid and its number is the account's verified
/// number, both taken from the sign-in token, never from here. Work photos are newest first.
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
    IReadOnlyList<string> WorkPhotoIds);

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
public sealed record SaveReviewRequest(string ProfessionalId, string ReviewerName, int Rating, string? Comment, string? ServiceId);

/// <summary><c>POST reviews/summaries</c>.</summary>
public sealed record ReviewSummariesRequest(IReadOnlyList<string> ProfessionalIds);

public sealed record ReviewSummaryDto(string ProfessionalId, double Average, int Count);

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
