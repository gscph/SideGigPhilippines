using SideGigPh.Contracts;

namespace SideGigPh.Admin.Components.Badge;

/// <summary>The operator's words for a badge request (English only: the site is for operators).</summary>
public static class BadgeText
{
    public static string Title(AdminVerificationDto request) => request.Kind switch
    {
        VerificationKinds.Id => "Government ID",
        VerificationKinds.Tesda => "TESDA certificate",
        VerificationKinds.Clearance => "NBI or police clearance",
        VerificationKinds.Developer => $"Past project with {Company(request)}",
        _ => request.Kind,
    };

    private static string Company(AdminVerificationDto request) => request.BadgeId switch
    {
        DeveloperBadges.AyalaLand => "Ayala Land",
        DeveloperBadges.Megaworld => "Megaworld",
        DeveloperBadges.RobinsonsLand => "Robinsons Land",
        DeveloperBadges.Sm => "SM",
        DeveloperBadges.Dmci => "DMCI",
        _ => string.IsNullOrWhiteSpace(request.CompanyName) ? "a company" : request.CompanyName,
    };

    /// <summary>The photo check's findings in words, or "Sharp, no glare or shadow found".</summary>
    public static string PhotoCheck(AdminVerificationDto request) =>
        request.PhotoProblems is { Count: > 0 } problems
            ? string.Join(" · ", problems.Select(Problem))
            : "Sharp, no glare or shadow found";

    private static string Problem(string code) => code switch
    {
        PhotoProblems.Blurry => "Blurry",
        PhotoProblems.Glare => "Glare",
        PhotoProblems.Dark => "Dark or shadow",
        PhotoProblems.TooSmall => "Too small",
        _ => code,
    };

    /// <summary>The photo check's blurry cut-off (the API's own, from the contracts).</summary>
    public const int BlurryBelow = PhotoProblems.BlurryBelow;

    /// <summary>The operator's reasons to reject, counted (<see cref="RejectCodes"/>), with the words that start the worker's reason.</summary>
    public static readonly IReadOnlyList<(string Code, string Label)> RejectReasons =
    [
        (RejectCodes.Blurry, "Blurry"),
        (RejectCodes.Glare, "Glare or shadow"),
        (RejectCodes.CutOff, "Cut off"),
        (RejectCodes.WrongDocument, "Wrong document"),
        (RejectCodes.Expired, "Expired or too old"),
        (RejectCodes.Mismatch, "Not the same person"),
        (RejectCodes.Other, "Other"),
    ];

    public static string Scan(AdminVerificationDto request) => request.Kind == VerificationKinds.Clearance ? "" : request.ScanStatus switch
    {
        ScanStatuses.Read => "AI read the ID (only a help: check by eye)",
        ScanStatuses.Unreadable => "AI couldn't read any ID fields",
        ScanStatuses.Failed => "AI reader didn't answer",
        _ => "",
    };

    /// <summary>What to check on an NBI or police clearance before approving it.</summary>
    public static readonly IReadOnlyList<string> ClearanceChecklist =
    [
        "The name matches the listing and the photo matches the profile photo.",
        "The remarks say \"No record\" (no criminal record).",
        "Recent: NBI issued less than 1 year ago, police clearance less than 6 months ago.",
        "Not edited: open the QR link and check the same name and number show.",
    ];

    /// <summary>What the server's read of the clearance's QR code says (only a hint: a QR can be copied onto a fake paper).</summary>
    public static string ClearanceQr(string? qr) => string.IsNullOrWhiteSpace(qr)
        ? "No QR code was read. Check the paper closely, or ask for a clearer photo."
        : ClearanceRules.IssuerOf(qr) switch
        {
            ClearanceIssuers.Nbi => "The QR code points to the NBI's website.",
            ClearanceIssuers.Police => "The QR code points to the PNP's clearance website.",
            _ => "The QR code doesn't point to an NBI or PNP website. Be careful: it may be fake.",
        };
}
