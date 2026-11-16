namespace SideGigPh.Contracts;

/// <summary>What a worker can be verified for (<c>POST verifications/{kind}</c>, <see cref="BadgeDto.Kind"/>).</summary>
public static class VerificationKinds
{
    /// <summary>A government ID: read by AI (name, number, expiry), then checked by a person. Badge: "ID Verified".</summary>
    public const string Id = "id";

    /// <summary>A TESDA National Certificate, checked by a person. Badge: "TESDA Certified".</summary>
    public const string Tesda = "tesda";

    /// <summary>Proof of work on a named developer's project (<see cref="DeveloperBadges"/>), checked by a person.</summary>
    public const string Developer = "developer";

    public static readonly IReadOnlyList<string> All = [Id, Tesda, Developer];
}

/// <summary>The developers a worker can show a "Worked on a … project" badge for. Ids are stable; only labels change.</summary>
public static class DeveloperBadges
{
    public const string AyalaLand = "ayala-land";
    public const string Megaworld = "megaworld";
    public const string RobinsonsLand = "robinsons-land";
    public const string Sm = "sm";
    public const string Dmci = "dmci";

    public static readonly IReadOnlyList<string> All = [AyalaLand, Megaworld, RobinsonsLand, Sm, Dmci];
}

public static class VerificationStatuses
{
    public const string Pending = "pending";
    public const string Approved = "approved";
    public const string Rejected = "rejected";
}

/// <summary>How the AI read of an ID went (<see cref="AdminVerificationDto.ScanStatus"/>).</summary>
public static class ScanStatuses
{
    /// <summary>Not an ID, or the reader isn't set up.</summary>
    public const string None = "none";

    /// <summary>At least one of name, number and expiry was read.</summary>
    public const string Read = "read";

    /// <summary>The reader answered but found no ID fields (a kind of ID it doesn't know, or a poor photo).</summary>
    public const string Unreadable = "unreadable";

    /// <summary>The reader couldn't be reached or timed out. The operator still checks the photo.</summary>
    public const string Failed = "failed";
}

/// <summary>
/// A badge on a listing: an approved verification. <paramref name="Kind"/> is one of <see cref="VerificationKinds"/>;
/// <paramref name="BadgeId"/> is the developer (<see cref="DeveloperBadges"/>) for a developer badge, else empty;
/// <paramref name="ProjectName"/> is the building the worker named, when given.
/// </summary>
public sealed record BadgeDto(string Kind, string BadgeId = "", string? ProjectName = null);

/// <summary>
/// One of the caller's own verification requests (<c>GET verifications/mine</c>, and the answer to
/// <c>POST verifications/{kind}</c>). Nothing read from the document comes back to the phone.
/// </summary>
public sealed record VerificationDto(
    long Id,
    string Kind,
    string BadgeId,
    string? ProjectName,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? DecidedAt,
    string? RejectReason);

/// <summary>
/// A request waiting for the operator (<c>GET admin/verifications</c>): who sent it, their profile photo to compare with
/// the ID by eye, and what the AI read. The document itself is <c>GET admin/verifications/{id}/document</c> (logged).
/// </summary>
public sealed record AdminVerificationDto(
    long Id,
    string Uid,
    string? WorkerName,
    string? ProfilePhotoUrl,
    string Kind,
    string BadgeId,
    string? ProjectName,
    string ScanStatus,
    string? ScannedType,
    string? ScannedName,
    string? ScannedNumber,
    DateOnly? ScannedExpiry,
    bool HasDocument,
    DateTimeOffset CreatedAt);

/// <summary><c>POST admin/verifications/{id}/decision</c>: approve (the badge shows) or reject with a short reason the worker sees.</summary>
public sealed record VerificationDecisionRequest(bool Approve, string? Reason = null);
