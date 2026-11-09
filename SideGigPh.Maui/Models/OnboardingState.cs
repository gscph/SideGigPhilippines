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

    public string? CityId { get; set; }

    public string? UrgencyId { get; set; }

    /// <summary>A price tier id, or <c>ReferenceData.Unsure</c>.</summary>
    public string? BudgetId { get; set; }
}

/// <summary>A Professional's listing as built during the interview (not published).</summary>
public sealed class ProfessionalDraft
{
    public List<string> CategoryIds { get; set; } = [];

    public List<string> ServiceIds { get; set; } = [];

    public List<string> CityIds { get; set; } = [];

    public string? ExperienceId { get; set; }

    /// <summary>A price tier id, or <c>ReferenceData.Quote</c>.</summary>
    public string? RateTierId { get; set; }

    /// <summary>Private monthly earnings goal; never shown on the listing.</summary>
    public string? IncomeGoalId { get; set; }

    public string? DisplayName { get; set; }

    /// <summary>E.164 format, e.g. +639171234567.</summary>
    public string? MobileNumber { get; set; }
}
