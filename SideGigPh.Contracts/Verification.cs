namespace SideGigPh.Contracts;

/// <summary>What a worker can be verified for (<c>POST verifications/{kind}</c>, <see cref="BadgeDto.Kind"/>).</summary>
public static class VerificationKinds
{
    /// <summary>A government ID: read by AI (name, number, expiry), then checked by a person. Badge: "ID Verified".</summary>
    public const string Id = "id";

    /// <summary>
    /// An NBI or police clearance, checked by a person; the server reads its QR code only as a hint for the operator
    /// (<see cref="AdminVerificationDto.ScannedQr"/>). The operator types its "valid until" date to approve it, and the
    /// badge lapses after that date (<see cref="ClearanceRules"/>). Badge: "NBI / Police Clearance".
    /// </summary>
    public const string Clearance = "clearance";

    public static readonly IReadOnlyList<string> All = [Id, Clearance];
}

public static class VerificationStatuses
{
    public const string Pending = "pending";
    public const string Approved = "approved";
    public const string Rejected = "rejected";
}

/// <summary>
/// The clearance badge's rules. An NBI clearance is valid 1 year from issue, a PNP police clearance 6 months: the
/// operator reads the "valid until" printed on it and types it when approving (never read by AI). The badge shows
/// until the end of that day, Philippine time, then lapses and the worker may send a new one.
/// </summary>
public static class ClearanceRules
{
    /// <summary>The longest a clearance is valid from its issue date (NBI); a police clearance is 6 months.</summary>
    public const int NbiValidMonths = 12;

    /// <summary>
    /// A "valid until" the operator may approve with: today or later (an expired paper is rejected instead), and at
    /// most a year and a week ahead (an NBI clearance issued today, with room for a date typed a few days off).
    /// </summary>
    public static bool IsValidUntilAllowed(DateOnly validUntil, DateOnly today) =>
        validUntil >= today && validUntil <= today.AddMonths(NbiValidMonths).AddDays(7);

    /// <summary>True when an approved badge with this "valid until" no longer shows: the day after it, Philippine time. Null (every other badge) never lapses.</summary>
    public static bool HasLapsed(DateOnly? validUntil, DateOnly today) => validUntil is { } until && until < today;

    /// <summary>The web addresses the NBI's clearance QR codes point to (and their subdomains).</summary>
    public static readonly IReadOnlyList<string> NbiHosts = ["nbi.gov.ph"];

    /// <summary>The web addresses the PNP's National Police Clearance System QR codes point to (and their subdomains).</summary>
    public static readonly IReadOnlyList<string> PoliceHosts = ["pnpclearance.ph", "pnp.gov.ph"];

    /// <summary>
    /// Who a clearance's QR code says issued it, from the web address it holds: <see cref="ClearanceIssuers.Nbi"/>,
    /// <see cref="ClearanceIssuers.Police"/>, or null (no QR, plain text, or an address that isn't the NBI's or
    /// PNP's, which the operator should treat as a warning sign). Only a hint: a QR can be copied onto a fake paper.
    /// </summary>
    public static string? IssuerOf(string? qr) => OfficialLink(qr) is not { } link ? null
        : IsHost(link.Host, NbiHosts) ? ClearanceIssuers.Nbi
        : ClearanceIssuers.Police;

    /// <summary>The QR's address when it is an NBI or PNP web address (http or https), for the operator to open and compare with the paper; else null.</summary>
    public static Uri? OfficialLink(string? qr) =>
        Uri.TryCreate(qr?.Trim(), UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp)
        && (IsHost(uri.Host, NbiHosts) || IsHost(uri.Host, PoliceHosts))
            ? uri
            : null;

    private static bool IsHost(string host, IReadOnlyList<string> hosts) =>
        hosts.Any(official => host.Equals(official, StringComparison.OrdinalIgnoreCase)
            || host.EndsWith("." + official, StringComparison.OrdinalIgnoreCase));
}

/// <summary>Who issued a clearance, as its QR code says (<see cref="ClearanceRules.IssuerOf"/>).</summary>
public static class ClearanceIssuers
{
    public const string Nbi = "nbi";
    public const string Police = "police";
}

/// <summary>How the AI read of an ID went (<see cref="AdminVerificationDto.ScanStatus"/>).</summary>
public static class ScanStatuses
{
    /// <summary>Not an ID, or the reader isn't set up.</summary>
    public const string None = "none";

    /// <summary>At least one of name, number and expiry was read (an ID), or a QR code was read (a clearance).</summary>
    public const string Read = "read";

    /// <summary>The reader answered but found no ID fields (a kind of ID it doesn't know, or a poor photo), or no QR code on a clearance.</summary>
    public const string Unreadable = "unreadable";

    /// <summary>The reader couldn't be reached or timed out. The operator still checks the photo.</summary>
    public const string Failed = "failed";
}

/// <summary>
/// What the server's photo check found on a badge proof (<see cref="VerificationDto.PhotoProblems"/>). Only advice:
/// the worker may take it again or send it anyway, and the operator sees it; nothing is refused for it.
/// </summary>
public static class PhotoProblems
{
    /// <summary>The text and edges aren't sharp (shaken, out of focus, or too far away).</summary>
    public const string Blurry = "blurry";

    /// <summary>A big patch is blown out white (a flash or a lamp reflecting on the card).</summary>
    public const string Glare = "glare";

    /// <summary>Most of the photo is dark (a shadow over the card, or not enough light).</summary>
    public const string Dark = "dark";

    /// <summary>The photo has too few pixels for small print to be read.</summary>
    public const string TooSmall = "too-small";

    public static readonly IReadOnlyList<string> All = [Blurry, Glare, Dark, TooSmall];

    /// <summary>
    /// The sharpness below which a photo counts as <see cref="Blurry"/> (the server's check), shown to the operator next
    /// to each photo's number. A first guess: set it from real photos against the operator's decisions.
    /// </summary>
    public const int BlurryBelow = 60;
}

/// <summary>Why the operator rejected a badge proof (<see cref="VerificationDecisionRequest.RejectCode"/>), counted to see what goes wrong most.</summary>
public static class RejectCodes
{
    public const string Blurry = "blurry";
    public const string Glare = "glare";
    public const string CutOff = "cut-off";
    public const string WrongDocument = "wrong-document";
    public const string Expired = "expired";
    public const string Mismatch = "mismatch";
    public const string Other = "other";

    public static readonly IReadOnlyList<string> All = [Blurry, Glare, CutOff, WrongDocument, Expired, Mismatch, Other];
}

/// <summary>A badge on a listing: an approved verification. <paramref name="Kind"/> is one of <see cref="VerificationKinds"/>.</summary>
public sealed record BadgeDto(string Kind);

/// <summary>
/// One of the caller's own verification requests (<c>GET verifications/mine</c>, and the answer to
/// <c>POST verifications/{kind}</c>). Nothing read from the document comes back to the phone.
/// </summary>
public sealed record VerificationDto(
    long Id,
    string Kind,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? DecidedAt,
    string? RejectReason,
    IReadOnlyList<string>? PhotoProblems = null,
    DateOnly? ValidUntil = null);

/// <summary>
/// A request waiting for the operator (<c>GET ops/verifications</c>): who sent it, their profile photo to compare with
/// the ID by eye, and what the AI read (<paramref name="ScannedQr"/>: a clearance's QR code as the server read it). The document itself is <c>GET ops/verifications/{id}/document</c> (logged).
/// </summary>
public sealed record AdminVerificationDto(
    long Id,
    string Uid,
    string? WorkerName,
    string? ProfilePhotoUrl,
    string Kind,
    string ScanStatus,
    string? ScannedType,
    string? ScannedName,
    string? ScannedNumber,
    DateOnly? ScannedExpiry,
    bool HasDocument,
    DateTimeOffset CreatedAt,
    IReadOnlyList<string>? PhotoProblems = null,
    double? PhotoSharpness = null,
    double? ScanConfidence = null,
    string? ScannedQr = null);

/// <summary><c>GET ops/me</c>: whether the caller is an operator (in the setting <c>AdminUids</c>), so the app shows the badge review screen.</summary>
public sealed record AdminStatusDto(bool IsOperator);

/// <summary>
/// <c>POST ops/verifications/{id}/decision</c>: approve (the badge shows) or reject with a short reason the worker sees
/// and, for counting, a <see cref="RejectCodes"/> code. Approving a clearance needs <paramref name="ValidUntil"/>, the date
/// printed on it (<see cref="ClearanceRules.IsValidUntilAllowed"/>).
/// </summary>
public sealed record VerificationDecisionRequest(bool Approve, string? Reason = null, string? RejectCode = null, DateOnly? ValidUntil = null);
