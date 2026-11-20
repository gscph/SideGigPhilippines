using SideGigPh.Contracts;

namespace SideGigPh.Admin.Components.Badge;

/// <summary>The operator's words for a badge request (English only: the site is for operators).</summary>
public static class BadgeText
{
    public static string Title(AdminVerificationDto request) => request.Kind switch
    {
        VerificationKinds.Id => "Government ID",
        VerificationKinds.Tesda => "TESDA certificate",
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

    public static string Scan(AdminVerificationDto request) => request.ScanStatus switch
    {
        ScanStatuses.Read => "AI read the ID (only a help: check by eye)",
        ScanStatuses.Unreadable => "AI couldn't read any ID fields",
        ScanStatuses.Failed => "AI reader didn't answer",
        _ => "",
    };
}
