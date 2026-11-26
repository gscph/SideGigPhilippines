namespace SideGigPh.Maui.Models;

/// <summary>
/// A usage event waiting on the phone to be uploaded (<c>Services/Analytics/TarbahoAnalytics</c>). Never a name, number
/// or place finer than a town; the account is added by the server from the sign-in token. The last three are newer:
/// a file kept by an older app has none (null).
/// </summary>
public sealed record PendingEvent(
    Guid EventId,
    string Name,
    string? SessionId,
    string? ServiceCategory,
    string? Area,
    string? SearchTerm,
    int? ResultCount,
    DateTimeOffset ClientTs,
    string? AppVersion,
    string? ServiceId = null,
    string? Step = null,
    string? UserType = null);

/// <summary>What <c>pending-events.json</c> holds: the events still to upload, oldest first.</summary>
public sealed class PendingEventsState
{
    public List<PendingEvent> Events { get; set; } = [];
}
