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

    /// <summary>An NBI or police clearance, checked by a person (no AI reads it). Badge: "NBI / Police Clearance".</summary>
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
    IReadOnlyList<string>? PhotoProblems = null);

/// <summary>
/// A request waiting for the operator (<c>GET ops/verifications</c>): who sent it, their profile photo to compare with
/// the ID by eye, and what the AI read. The document itself is <c>GET ops/verifications/{id}/document</c> (logged).
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
    IReadOnlyList<string>? PhotoProblems = null,
    double? PhotoSharpness = null,
    double? ScanConfidence = null);

/// <summary><c>GET ops/me</c>: whether the caller is an operator (in the setting <c>AdminUids</c>), so the app shows the badge review screen.</summary>
public sealed record AdminStatusDto(bool IsOperator);

/// <summary>
/// <c>POST ops/verifications/{id}/decision</c>: approve (the badge shows) or reject with a short reason the worker sees
/// and, for counting, a <see cref="RejectCodes"/> code.
/// </summary>
public sealed record VerificationDecisionRequest(bool Approve, string? Reason = null, string? RejectCode = null);
