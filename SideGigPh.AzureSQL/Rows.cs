namespace SideGigPh.AzureSQL;

// One class per table in Sql/001-schema.sql and the later NNN-schema-*.sql changes. Only this project sees them: the API answers with the
// SideGigPh.Contracts records, which the stores map to.

public sealed class UserRow
{
    public required string Uid { get; set; }
    public string? DisplayName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public required string Language { get; set; }
    public string? Role { get; set; }
    public string? IncomeGoalId { get; set; }
    public string? ProfilePhotoId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? LastActiveAt { get; set; }
}

public sealed class ProfessionalRow
{
    public required string Id { get; set; }
    public required string DisplayName { get; set; }
    public required string MobileNumber { get; set; }
    public required string CategoryId { get; set; }
    public required string AreaId { get; set; }
    public string? ExperienceId { get; set; }
    public string? RateTierId { get; set; }
    public string? ProfilePhotoId { get; set; }
    public DateTimeOffset PublishedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>
    /// The worker's base point (<c>013-schema-geo.sql</c>), private: never sent to another person. Null = not known
    /// (matched by town only). The database keeps the same point as the geography column <c>Location</c>, which
    /// distance queries read (<see cref="ProfessionalStore.WorkersInRangeAsync"/>).
    /// </summary>
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    /// <summary>How far the worker travels from that point, <see cref="GeoRules.MinRadiusKm"/> to <see cref="GeoRules.MaxRadiusKm"/>.</summary>
    public short RadiusKm { get; set; } = GeoRules.DefaultRadiusKm;

    public List<ProfessionalServiceRow> Services { get; set; } = [];
    public List<ProfessionalCityRow> Cities { get; set; } = [];
    public List<WorkPhotoRow> WorkPhotos { get; set; } = [];
}

public sealed class ProfessionalServiceRow
{
    public required string ProfessionalId { get; set; }
    public required string ServiceId { get; set; }
}

public sealed class ProfessionalCityRow
{
    public required string ProfessionalId { get; set; }
    public required string CityId { get; set; }
}

public sealed class WorkPhotoRow
{
    public required string PhotoId { get; set; }
    public required string ProfessionalId { get; set; }

    /// <summary>0 = newest.</summary>
    public short Position { get; set; }
}

/// <summary>Someone who can vouch for a worker. Private: never sent to Seekers.</summary>
public sealed class ReferenceRow
{
    public required string ProfessionalId { get; set; }

    /// <summary>0 = first added.</summary>
    public short Position { get; set; }

    public required string Name { get; set; }

    /// <summary>E.164, e.g. +639171234567.</summary>
    public required string MobileNumber { get; set; }
}

public sealed class ReviewRow
{
    public required string ProfessionalId { get; set; }
    public required string SeekerId { get; set; }
    public required string ReviewerName { get; set; }
    public byte Rating { get; set; }
    public string? Comment { get; set; }
    public string? ServiceId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class ChatRow
{
    public required string Id { get; set; }
    public required string ProfessionalId { get; set; }
    public required string SeekerId { get; set; }
    public required string ProfessionalName { get; set; }
    public required string SeekerName { get; set; }
    public string? ServiceId { get; set; }
    public string? CityId { get; set; }
    public string? UrgencyId { get; set; }
    public string? BudgetId { get; set; }
    public required string LastMessageId { get; set; }
    public required string LastMessageSenderId { get; set; }
    public required string LastMessageKind { get; set; }
    public string? LastMessageText { get; set; }
    public DateTimeOffset LastMessageAt { get; set; }
    public DateTimeOffset? SeekerReadAt { get; set; }
    public DateTimeOffset? ProfessionalReadAt { get; set; }
    public DateTimeOffset? ProfessionalRepliedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class ChatMessageRow
{
    public required string Id { get; set; }
    public required string ChatId { get; set; }
    public required string SenderId { get; set; }
    public required string Kind { get; set; }
    public string? Text { get; set; }
    public DateTimeOffset At { get; set; }
}

public sealed class PushDeviceRow
{
    public required string Token { get; set; }
    public required string Uid { get; set; }
    public required string Platform { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>A trade (<c>Categories</c>). <see cref="IsActive"/> false = retired: kept for old listings, no longer offered.</summary>
public sealed class CategoryRow
{
    public required string Id { get; set; }
    public required string LabelEnglish { get; set; }
    public required string LabelTaglish { get; set; }
    public string? Icon { get; set; }
    public string? HintEnglish { get; set; }
    public string? HintTaglish { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>A job within a trade (<c>Services</c>).</summary>
public sealed class ServiceRow
{
    public required string Id { get; set; }
    public required string CategoryId { get; set; }
    public required string LabelEnglish { get; set; }
    public required string LabelTaglish { get; set; }
    public string? Icon { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }

    /// <summary>The job's price guide in pesos (labor only), added by <c>008-schema-service-prices.sql</c>; null on retired jobs.</summary>
    public int? PriceMin { get; set; }
    public int? PriceAverage { get; set; }
    public int? PriceHigh { get; set; }
}

/// <summary>What someone typed after picking "Others" (<c>OtherJobNotes</c>), for analytics.</summary>
public sealed class OtherJobNoteRow
{
    public long Id { get; set; }
    public string? Uid { get; set; }
    public required string Side { get; set; }
    public required string CategoryId { get; set; }
    public required string Text { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>A problem the app had on a phone (<c>ErrorReports</c>): diagnostics with no personal data, kept 90 days.</summary>
public sealed class ErrorReportRow
{
    public long Id { get; set; }
    public required string InstallId { get; set; }
    public required string AppVersion { get; set; }
    public required string CodeVersion { get; set; }
    public required string Channel { get; set; }
    public required string Platform { get; set; }
    public string? Device { get; set; }
    public required string Kind { get; set; }
    public required string Source { get; set; }
    public required string Message { get; set; }
    public string? Detail { get; set; }
    public string? Route { get; set; }
    public string? Trail { get; set; }
    public int Count { get; set; }
    public DateTimeOffset FirstAt { get; set; }
    public DateTimeOffset LastAt { get; set; }
    public DateTimeOffset ReceivedAt { get; set; }
}

/// <summary>One answer in a config list (<c>ConfigOptions</c>): a town, price tier, experience, urgency or income goal.</summary>
public sealed class ConfigOptionRow
{
    public required string ListId { get; set; }
    public required string Id { get; set; }
    public required string LabelEnglish { get; set; }
    public required string LabelTaglish { get; set; }
    public string? Icon { get; set; }
    public int Amount { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>A user reporting another user's listing, review, chat or account (<c>Reports</c>); the operator answers within 5 business days.</summary>
public sealed class ReportRow
{
    public long Id { get; set; }
    public string? ReporterUid { get; set; }
    public required string TargetUid { get; set; }
    public required string Kind { get; set; }
    public string? SubjectId { get; set; }
    public required string Reason { get; set; }
    public string? Note { get; set; }
    public string Status { get; set; } = "open";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }
}

/// <summary>A feature someone suggested on the feedback board (<c>FeedbackPosts</c>), shown without the writer's name.</summary>
public sealed class FeedbackPostRow
{
    public long Id { get; set; }
    public string? AuthorUid { get; set; }
    public string? Side { get; set; }
    public required string Title { get; set; }
    public string? Details { get; set; }

    /// <summary>How many <see cref="FeedbackVoteRow"/>s it has, kept here so the board sorts without counting.</summary>
    public int Votes { get; set; }
    public string Status { get; set; } = "open";
    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>One person's vote for a suggestion (<c>FeedbackVotes</c>): at most one each.</summary>
public sealed class FeedbackVoteRow
{
    public long PostId { get; set; }
    public required string VoterUid { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>One user blocked another (<c>Blocks</c>); either direction hides the two from each other.</summary>
public sealed class BlockRow
{
    public required string BlockerUid { get; set; }
    public required string BlockedUid { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>
/// A proof a worker sent for a badge (<c>WorkerVerifications</c>, <c>014-schema-verification.sql</c>): a government ID,
/// a TESDA certificate or proof of work on a named developer's project. A person decides; the file and what was read
/// from it are deleted then, and only the result stays.
/// </summary>
public sealed class WorkerVerificationRow
{
    public long Id { get; set; }
    public required string Uid { get; set; }
    public required string Kind { get; set; }

    /// <summary>The developer's id for a developer badge, else empty.</summary>
    public string BadgeId { get; set; } = string.Empty;
    public string? ProjectName { get; set; }
    public string Status { get; set; } = "pending";

    /// <summary>The file's name in the private container; null once deleted.</summary>
    public string? DocumentBlob { get; set; }
    public string ScanStatus { get; set; } = "none";
    public string? ScannedType { get; set; }
    public string? ScannedName { get; set; }
    public string? ScannedNumber { get; set; }
    public DateOnly? ScannedExpiry { get; set; }
    public DateTimeOffset ConsentAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? DecidedAt { get; set; }
    public string? DecidedBy { get; set; }
    public string? RejectReason { get; set; }
}

/// <summary>One action on a verification request (<c>VerificationAudit</c>): the log of who looked at or decided what.</summary>
public sealed class VerificationAuditRow
{
    public long Id { get; set; }
    public long VerificationId { get; set; }
    public required string ActorUid { get; set; }
    public required string Action { get; set; }
    public DateTimeOffset At { get; set; }
}
