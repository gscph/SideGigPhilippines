namespace SideGigPh.AzureSQL;

// One class per table in Sql/001-schema.sql. Only this project sees them: the API answers with the
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
