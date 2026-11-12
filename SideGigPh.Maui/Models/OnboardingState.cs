namespace SideGigPh.Maui.Models;

/// <summary>
/// Everything the onboarding interview has collected, saved on the device with no account.
/// Ids refer to entries in <c>ReferenceData</c>.
/// </summary>
public sealed class OnboardingState
{
    public AppLanguage? Language { get; set; }

    public UserRole? Role { get; set; }

    /// <summary>The interview step to reopen if the app is closed mid-interview.</summary>
    public string? ResumePath { get; set; }

    public SeekerAnswers Seeker { get; set; } = new();

    public ProfessionalDraft Professional { get; set; } = new();

    /// <summary>Set when the user finishes the interview for their current role.</summary>
    public DateTimeOffset? CompletedAt { get; set; }
}

/// <summary>A household's answers: what job, where, when and for how much.</summary>
public sealed class SeekerAnswers
{
    public string? CategoryId { get; set; }

    public string? ServiceId { get; set; }

    /// <summary>The area of their home town (<c>ReferenceData.AreaOf</c>), never asked: set with <see cref="CityId"/>.</summary>
    public string? AreaId { get; set; }

    /// <summary>Their one home town (<c>ReferenceData.Cities</c>).</summary>
    public string? CityId { get; set; }

    public string? UrgencyId { get; set; }

    /// <summary>A price tier id, or <c>ReferenceData.Unsure</c>.</summary>
    public string? BudgetId { get; set; }

    /// <summary>What they last typed in the search box, shown again when they come back.</summary>
    public string? SearchText { get; set; }

    /// <summary>The name they put on their reviews, e.g. "Maricel D.".</summary>
    public string? ReviewerName { get; set; }

    /// <summary>Their one profile photo (SHA-256 id). A new one replaces it and the old one is deleted.</summary>
    public string? ProfilePhotoId { get; set; }
}

/// <summary>A Professional's listing: built in the interview, then photos and publishing on Pro home.</summary>
public sealed class ProfessionalDraft
{
    /// <summary>The one trade the Professional works in. A pro picks a single category.</summary>
    public string? CategoryId { get; set; }

    public List<string> ServiceIds { get; set; } = [];

    /// <summary>The one area they work in (<c>ReferenceData.AreaOf</c> their towns), never asked: set with <see cref="CityIds"/>.</summary>
    public string? AreaId { get; set; }

    /// <summary>The towns they go to (<c>ReferenceData.Cities</c>), several allowed.</summary>
    public List<string> CityIds { get; set; } = [];

    public string? ExperienceId { get; set; }

    /// <summary>A price tier id, or <c>ReferenceData.Quote</c>.</summary>
    public string? RateTierId { get; set; }

    /// <summary>Private monthly earnings goal; never shown on the listing.</summary>
    public string? IncomeGoalId { get; set; }

    public string? DisplayName { get; set; }

    /// <summary>E.164 format, e.g. +639171234567.</summary>
    public string? MobileNumber { get; set; }

    /// <summary>The pro's own photo, an <c>IPhotoStore</c> id.</summary>
    public string? ProfilePhotoId { get; set; }

    /// <summary>Photos of their work, <c>IPhotoStore</c> ids, newest first.</summary>
    public List<string> WorkPhotoIds { get; set; } = [];

    /// <summary>People who can vouch for the pro (private, never shown to customers), first added first.</summary>
    public List<WorkReference> References { get; set; } = [];

    /// <summary>Set when the pro took the pledge and published the listing.</summary>
    public DateTimeOffset? PublishedAt { get; set; }

    /// <summary>
    /// Has every answer the listing shows: trade, at least one job, area, at least one town, experience and rate.
    /// Only then can it be published (the API refuses a listing without trade, area, jobs and towns).
    /// </summary>
    public bool IsListingComplete =>
        CategoryId is not null && ServiceIds.Count > 0
        && AreaId is not null && CityIds.Count > 0
        && ExperienceId is not null && RateTierId is not null;

    public bool HasProfilePhoto => ProfilePhotoId is not null;

    /// <summary>At least <see cref="MinWorkPhotos"/> work photos.</summary>
    public bool HasEnoughWorkPhotos => WorkPhotoIds.Count >= MinWorkPhotos;

    public bool HasReference => References.Count >= MinReferences;

    /// <summary>
    /// The setup checklist is done: own photo, at least 2 work photos and at least 1 reference (the API refuses a
    /// listing without them). Together with <see cref="IsListingComplete"/> the listing can be published.
    /// </summary>
    public bool HasProof => HasProfilePhoto && HasEnoughWorkPhotos && HasReference;

    /// <summary>Fewest work photos a listing can be published with (the API's <c>ProfessionalStore.MinWorkPhotos</c>).</summary>
    public const int MinWorkPhotos = 2;

    /// <summary>Fewest and most references (the API's <c>ProfessionalStore.MinReferences</c> / <c>MaxReferences</c>).</summary>
    public const int MinReferences = 1;
    public const int MaxReferences = 3;
}

/// <summary>Someone who can vouch for a Professional: a past customer or employer.</summary>
/// <param name="MobileNumber">E.164 format, e.g. +639171234567.</param>
public sealed record WorkReference(string Name, string MobileNumber);
