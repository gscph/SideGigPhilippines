namespace SideGigPh.Maui.Models;

/// <summary>How the AI read of an ID went (only to help the operator: a person always decides).</summary>
public enum BadgeScanResult
{
    /// <summary>Not an ID, or the reader isn't set up.</summary>
    None,

    /// <summary>At least one of name, number and expiry was read, or a clearance's QR code.</summary>
    Read,

    /// <summary>The reader found no ID fields, or no QR code on a clearance.</summary>
    Unreadable,

    /// <summary>The reader couldn't be reached.</summary>
    Failed,
}

/// <summary>Who issued a clearance, as its QR code's web address says.</summary>
public enum ClearanceIssuer
{
    Nbi,
    Police,
}

/// <summary>
/// A clearance's QR code as the server read it: its <paramref name="Text"/>, who it says issued the paper (null when
/// the address isn't the NBI's or PNP's: a warning sign) and that address to open and compare with the paper.
/// </summary>
public sealed record ClearanceQr(string Text, ClearanceIssuer? Issuer, string? Link);

/// <summary>
/// What was read from a proof: an ID's fields by AI (with its <paramref name="Confidence"/>), or a clearance's QR code
/// (<paramref name="Qr"/>) by the server. Shown only to the operator, never sent to any other phone.
/// </summary>
public sealed record BadgeScan(
    BadgeScanResult Result, string? Type = null, string? Name = null, string? Number = null, DateOnly? Expiry = null, double? Confidence = null,
    ClearanceQr? Qr = null);

/// <summary>The server's photo check on a proof: what it found and its sharpness number (to compare with the photo by eye).</summary>
public sealed record PhotoCheck(IReadOnlyList<ProofProblem> Problems, double? Sharpness);

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
    DateTimeOffset SentAt,
    PhotoCheck? Photo = null);
