namespace SideGigPh.Maui.Models;

/// <summary>How a Seeker reached out to a Professional.</summary>
public enum InquiryChannel
{
    Call,
    Text,
}

/// <summary>
/// A Seeker called or texted a Professional from the app. The call or text itself happens in
/// the phone's own apps, so this records the attempt, the text they sent and their search
/// answers. It is the pro's inquiry and, grouped by pro, the Seeker's inbox.
/// Ids refer to entries in <c>ReferenceData</c>.
/// </summary>
public sealed record Inquiry(
    string Id,
    string ProfessionalId,
    string SeekerId,
    InquiryChannel Channel,
    DateTimeOffset At,
    string? Message,
    string? ServiceId,
    string? CityId,
    string? UrgencyId,
    string? BudgetId);
