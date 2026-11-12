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

    /// <summary>The area (<c>ReferenceData.Areas</c>) of their home, picked before the town.</summary>
    public string? AreaId { get; set; }

    /// <summary>Their home town, inside <see cref="AreaId"/>.</summary>
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

    /// <summary>The one area (<c>ReferenceData.Areas</c>: Metro Manila, Rizal, Laguna, Cavite) they work in.</summary>
    public string? AreaId { get; set; }

    /// <summary>The towns they go to, all inside <see cref="AreaId"/>.</summary>
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
}
