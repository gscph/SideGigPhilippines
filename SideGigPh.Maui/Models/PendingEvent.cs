namespace SideGigPh.Maui.Models;

/// <summary>
/// A usage event waiting on the phone to be uploaded (<c>Services/Analytics/TarbahoAnalytics</c>). Never a name, number
/// or place finer than a town; the account is added by the server from the sign-in token.
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
    string? AppVersion);

/// <summary>What <c>pending-events.json</c> holds: the events still to upload, oldest first.</summary>
public sealed class PendingEventsState
{
    public List<PendingEvent> Events { get; set; } = [];
}
