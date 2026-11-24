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

    /// <summary>The worker's average price per service, in pesos (<c>023-schema-worker-price.sql</c>).</summary>
    public int RatePesos { get; set; }

    /// <summary>What the worker typed under "Others" (<c>017-schema-others-and-ranges.sql</c>), matched in search.</summary>
    public string? OtherWorkText { get; set; }

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

/// <summary>
/// Someone who vouched for a worker, from before the references feature was dropped. Nothing reads or writes these
/// rows any more except Delete account, which purges them; the table goes in a later guarded drop script.
/// </summary>
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

    /// <summary>The job's names when the chat started or a pick moved it (<c>029-schema-catalog-names.sql</c>): an operator's rename never rewrites them.</summary>
    public string? ServiceLabelEnglish { get; set; }
    public string? ServiceLabelTaglish { get; set; }

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

    /// <summary>When an operator last changed it in the operator site (<c>029-schema-catalog-names.sql</c>); set = <c>004-config.sql</c> leaves it alone.</summary>
    public DateTimeOffset? EditedAt { get; set; }

    /// <summary>The operator's uid behind <see cref="EditedAt"/>.</summary>
    public string? EditedBy { get; set; }
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

    /// <summary>When an operator last changed it in the operator site (<c>029-schema-catalog-names.sql</c>); set = <c>004-config.sql</c> leaves it alone.</summary>
    public DateTimeOffset? EditedAt { get; set; }

    /// <summary>The operator's uid behind <see cref="EditedAt"/>.</summary>
    public string? EditedBy { get; set; }
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

/// <summary>A landing page Early Access sign-up (<c>EarlyAccessSignups</c>), one per email address.</summary>
public sealed class EarlyAccessSignupRow
{
    public required string Email { get; set; }
    public required string Phone { get; set; }
    public string? Role { get; set; }
    public string? Language { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
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

/// <summary>One usage event (<c>app_events</c>, <see cref="AppEventStore"/>): no personal data, kept 12 months.</summary>
public sealed class AppEventRow
{
    public long Id { get; set; }
    public Guid EventId { get; set; }
    public required string EventName { get; set; }
    public required string Source { get; set; }
    public string? UserId { get; set; }
    public string? UserType { get; set; }
    public string? SessionId { get; set; }
    public string? ServiceCategory { get; set; }
    public string? Area { get; set; }
    public string? SearchTerm { get; set; }
    public int? ResultCount { get; set; }
    public int? MinutesToMatch { get; set; }
    public string? Reason { get; set; }
    public string? RequestId { get; set; }
    public string? AppVersion { get; set; }
    public DateTime? ClientTs { get; set; }
    public DateTime ServerTs { get; set; }
}

/// <summary>One answer in a config list (<c>ConfigOptions</c>): a town, the default town, experience, urgency or income goal.</summary>
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

/// <summary>
/// A barangay we serve in a town (<c>Barangays</c>, <c>021-schema-barangays.sql</c>, filled by <c>004-config.sql</c>):
/// <c>angono-san-isidro</c> "San Isidro" in <c>angono</c>. Ids are stable; a barangay we stop serving is retired.
/// </summary>
public sealed class BarangayRow
{
    public required string Id { get; set; }
    public required string CityId { get; set; }
    public required string LabelEnglish { get; set; }
    public required string LabelTaglish { get; set; }
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

    /// <summary>Where it was written (<see cref="FeedbackSources"/>, <c>025-schema-feedback-source.sql</c>).</summary>
    public string Source { get; set; } = FeedbackSources.App;
    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>One person's vote for a suggestion (<c>FeedbackVotes</c>): at most one each.</summary>
public sealed class FeedbackVoteRow
{
    public long PostId { get; set; }
    public required string VoterUid { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>
/// The work a household needs done (<c>JobPosts</c>, <c>015-schema-job-posts.sql</c>). The point is private: workers
/// get the town and a rounded distance. The database keeps the same point as the geography column <c>Location</c>,
/// read only in SQL.
/// </summary>
public sealed class JobPostRow
{
    public long Id { get; set; }
    public required string SeekerUid { get; set; }
    public required string CategoryId { get; set; }
    public required string ServiceId { get; set; }
    public required string Description { get; set; }

    /// <summary>The household's own title, the post's header (<c>027-schema-post-title.sql</c>); null only on older posts.</summary>
    public string? Title { get; set; }

    /// <summary>
    /// The trade's and job's names when the post was made or its job last changed (<c>029-schema-catalog-names.sql</c>),
    /// like an order line keeping a product's title: an operator's rename never rewrites a post. Null for "Others".
    /// </summary>
    public string? TradeLabelEnglish { get; set; }
    public string? TradeLabelTaglish { get; set; }
    public string? ServiceLabelEnglish { get; set; }
    public string? ServiceLabelTaglish { get; set; }

    /// <summary>The top of the budget ("up to").</summary>
    public int BudgetPesos { get; set; }

    /// <summary>The low end of the budget ("from", <c>017-schema-others-and-ranges.sql</c>); null = only a top was given.</summary>
    public int? BudgetMinPesos { get; set; }

    public required string CityId { get; set; }

    /// <summary>The barangay the household picked (<c>021-schema-barangays.sql</c>); null = not sure or not given.</summary>
    public string? BarangayId { get; set; }

    /// <summary>The day the household needs it done (<c>024-schema-post-needed-date.sql</c>); null = any day.</summary>
    public DateOnly? NeededOn { get; set; }

    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string Status { get; set; } = "open";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
    public List<JobPostPhotoRow> Photos { get; set; } = [];
}

/// <summary>A photo of the problem on a job post (<c>JobPostPhotos</c>).</summary>
public sealed class JobPostPhotoRow
{
    public long PostId { get; set; }
    public required string PhotoId { get; set; }

    /// <summary>0 = first added.</summary>
    public short Position { get; set; }
}

/// <summary>
/// A worker's offer on a job post (<c>JobOffers</c>, <c>016-schema-job-offers.sql</c>): the household's budget or
/// the worker's own price, one per worker per post.
/// </summary>
public sealed class JobOfferRow
{
    public long Id { get; set; }
    public long PostId { get; set; }
    public required string ProUid { get; set; }
    public int PricePesos { get; set; }
    public string? Note { get; set; }
    public string Status { get; set; } = "sent";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>When the household marked the picked job done (<c>018-schema-job-done.sql</c>).</summary>
    public DateTimeOffset? HouseholdDoneAt { get; set; }

    /// <summary>When the worker marked the picked job done. Both set: a completed job.</summary>
    public DateTimeOffset? WorkerDoneAt { get; set; }

    /// <summary>When a picked job was called off (<c>026-schema-cancel-pick.sql</c>, status <c>cancelled</c>).</summary>
    public DateTimeOffset? CancelledAt { get; set; }

    /// <summary>Who called it off: <c>household</c> or <c>worker</c> (<see cref="SideGigPh.Contracts.PickCancelSides"/>).</summary>
    public string? CancelledBy { get; set; }

    /// <summary>Why, an id from <see cref="SideGigPh.Contracts.PickCancelReasons"/>.</summary>
    public string? CancelReason { get; set; }

    /// <summary>Optional words with the reason.</summary>
    public string? CancelNote { get; set; }

    /// <summary>
    /// The exact point the household sent the picked worker (<c>028-schema-meet-location.sql</c>). Private to the two,
    /// cleared when the job is called off or completed.
    /// </summary>
    public double? MeetLatitude { get; set; }

    public double? MeetLongitude { get; set; }

    /// <summary>When the household sent it.</summary>
    public DateTimeOffset? MeetSharedAt { get; set; }
}

/// <summary>
/// A worker's stars for a household (<c>HouseholdReviews</c>, <c>018-schema-job-done.sql</c>): one per worker per
/// household, only after a completed job between them.
/// </summary>
public sealed class HouseholdReviewRow
{
    public required string ProUid { get; set; }
    public required string SeekerUid { get; set; }
    public byte Rating { get; set; }
    public string? Comment { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
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
/// an NBI or police clearance, a TESDA certificate or proof of a past project with a developer or another company. A person decides; the file and what was read
/// from it are deleted then, and only the result stays.
/// </summary>
public sealed class WorkerVerificationRow
{
    public long Id { get; set; }
    public required string Uid { get; set; }
    public required string Kind { get; set; }

    /// <summary>A project badge's developer id (<c>DeveloperBadges</c>, or "other"), else empty.</summary>
    public string BadgeId { get; set; } = string.Empty;

    /// <summary>The company the worker typed for a project badge with "other", else empty.</summary>
    public string CompanyName { get; set; } = string.Empty;
    public string? ProjectName { get; set; }
    public string Status { get; set; } = "pending";

    /// <summary>The file's name in the private container; null once deleted.</summary>
    public string? DocumentBlob { get; set; }
    public string ScanStatus { get; set; } = "none";
    public string? ScannedType { get; set; }
    public string? ScannedName { get; set; }
    public string? ScannedNumber { get; set; }
    public DateOnly? ScannedExpiry { get; set; }

    /// <summary>What the server read from a clearance's QR code (<c>033-schema-clearance-valid-until.sql</c>), for the operator only; cleared on the decision.</summary>
    public string? ScannedQr { get; set; }

    /// <summary>A clearance's "valid until", typed by the operator on approving it; the badge lapses after it. Kept after the decision.</summary>
    public DateOnly? ValidUntil { get; set; }
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
