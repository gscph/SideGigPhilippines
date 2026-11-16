using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services;

/// <summary>
/// The well-known developers a worker can pick for a project badge (names every household knows), and the rules for
/// naming any other company. Ids are stable and match the server's; the names are brand names, the same in both languages.
/// </summary>
public static class BadgeCatalog
{
    /// <summary>The id for "another company": the worker types its name.</summary>
    public const string OtherCompany = "other";

    public const int MinCompanyName = 2;
    public const int MaxCompanyName = 60;
    public const int MaxProjectName = 100;

    /// <summary>How many kinds of badge there are (ID, TESDA, past project).</summary>
    public const int Kinds = 3;

    /// <summary>The developers offered, in the order shown.</summary>
    public static readonly IReadOnlyList<(string Id, string Name)> Developers =
    [
        ("ayala-land", "Ayala Land"),
        ("megaworld", "Megaworld"),
        ("robinsons-land", "Robinsons Land"),
        ("sm", "SM"),
        ("dmci", "DMCI"),
    ];

    /// <summary>The company a project badge names: the developer's name, or what the worker typed. Null when neither is known.</summary>
    public static string? CompanyOf(Badge badge) =>
        Developers.FirstOrDefault(d => d.Id == badge.DeveloperId).Name
        ?? (string.IsNullOrWhiteSpace(badge.CompanyName) ? null : badge.CompanyName.Trim());

    /// <summary>A project badge can be asked for: a developer picked, or another company with a name long enough.</summary>
    public static bool HasCompany(Badge badge) =>
        badge.Kind != BadgeKind.Project
        || Developers.Any(d => d.Id == badge.DeveloperId)
        || (badge.DeveloperId == OtherCompany && badge.CompanyName?.Trim().Length is >= MinCompanyName and <= MaxCompanyName);
}
