namespace SideGigPh.Maui.Models;

/// <summary>How the AI read of an ID went (only to help the operator: a person always decides).</summary>
public enum BadgeScanResult
{
    /// <summary>Not an ID, or the reader isn't set up.</summary>
    None,

    /// <summary>At least one of name, number and expiry was read.</summary>
    Read,

    /// <summary>The reader found no ID fields.</summary>
    Unreadable,

    /// <summary>The reader couldn't be reached.</summary>
    Failed,
}

/// <summary>What the AI read from an ID: shown only to the operator, never sent to any other phone.</summary>
public sealed record BadgeScan(BadgeScanResult Result, string? Type = null, string? Name = null, string? Number = null, DateOnly? Expiry = null);

/// <summary>
/// A worker's badge request waiting for the operator: who sent it, their profile photo (to compare with the ID by
/// eye), the badge asked for and what the AI read. The proof photo itself is fetched only when the operator opens it.
/// </summary>
public sealed record PendingBadge(
    long Id,
    string WorkerUid,
    string WorkerName,
    string? PhotoUrl,
    Badge Badge,
    BadgeScan Scan,
    bool HasDocument,
    DateTimeOffset SentAt);
