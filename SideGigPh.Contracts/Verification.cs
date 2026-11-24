namespace SideGigPh.Contracts;

/// <summary>What a worker can be verified for (<c>POST verifications/{kind}</c>, <see cref="BadgeDto.Kind"/>).</summary>
public static class VerificationKinds
{
    /// <summary>A government ID: read by AI (name, number, expiry), then checked by a person. Badge: "ID Verified".</summary>
    public const string Id = "id";

    /// <summary>A TESDA National Certificate or training certificate, checked by a person. Badge: "TESDA Certified".</summary>
    public const string Tesda = "tesda";

    /// <summary>
    /// Proof of a past project with a developer or any other company, checked by a person: a well-known developer
    /// by id (<see cref="DeveloperBadges"/>), or <see cref="DeveloperBadges.Other"/> with the company's name typed.
    /// </summary>
    public const string Developer = "developer";

    /// <summary>
    /// An NBI or police clearance, checked by a person; the server reads its QR code only as a hint for the operator
    /// (<see cref="AdminVerificationDto.ScannedQr"/>). The operator types its "valid until" date to approve it, and the
    /// badge lapses after that date (<see cref="ClearanceRules"/>). Badge: "NBI / Police Clearance".
    /// </summary>
    public const string Clearance = "clearance";

    public static readonly IReadOnlyList<string> All = [Id, Tesda, Developer, Clearance];
}

/// <summary>
/// The well-known developers a worker can pick for a "Worked on a … project" badge (household names that carry
/// trust at a glance), and <see cref="Other"/> for any other company, typed by the worker. Ids are stable.
/// </summary>
public static class DeveloperBadges
{
    public const string AyalaLand = "ayala-land";
    public const string Megaworld = "megaworld";
    public const string RobinsonsLand = "robinsons-land";
    public const string Sm = "sm";
    public const string Dmci = "dmci";

    /// <summary>Any other company: its name comes with the request (<c>companyName</c>).</summary>
    public const string Other = "other";

    /// <summary>The shortest and longest company name a worker may type.</summary>
    public const int MinCompanyName = 2;
    public const int MaxCompanyName = 60;

    public static readonly IReadOnlyList<string> All = [AyalaLand, Megaworld, RobinsonsLand, Sm, Dmci, Other];
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
    public const int PoliceValidMonths = 6;

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
/// A badge on a listing: an approved verification. <paramref name="Kind"/> is one of <see cref="VerificationKinds"/>;
/// <paramref name="BadgeId"/> is the developer (<see cref="DeveloperBadges"/>) for a project badge, else empty;
/// <paramref name="CompanyName"/> is the company the worker typed when that is <see cref="DeveloperBadges.Other"/>;
/// <paramref name="ProjectName"/> is the building the worker named, when given.
/// </summary>
public sealed record BadgeDto(string Kind, string BadgeId = "", string? ProjectName = null, string? CompanyName = null);

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
    string? RejectReason,
    string? CompanyName = null,
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
    string BadgeId,
    string? ProjectName,
    string ScanStatus,
    string? ScannedType,
    string? ScannedName,
    string? ScannedNumber,
    DateOnly? ScannedExpiry,
    bool HasDocument,
    DateTimeOffset CreatedAt,
    string? CompanyName = null,
    string? ScannedQr = null);

/// <summary><c>GET ops/me</c>: whether the caller is an operator (in the setting <c>AdminUids</c>), so the app shows the badge review screen.</summary>
public sealed record AdminStatusDto(bool IsOperator);

/// <summary>
/// <c>POST ops/verifications/{id}/decision</c>: approve (the badge shows) or reject with a short reason the worker sees.
/// Approving a clearance needs <paramref name="ValidUntil"/>, the date printed on it (<see cref="ClearanceRules.IsValidUntilAllowed"/>).
/// </summary>
public sealed record VerificationDecisionRequest(bool Approve, string? Reason = null, DateOnly? ValidUntil = null);
