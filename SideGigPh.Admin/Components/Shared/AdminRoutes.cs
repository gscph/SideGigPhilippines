namespace SideGigPh.Admin.Components.Shared;

/// <summary>
/// The site's pages. Relative on purpose (no leading slash): the site lives under the API at <c>…/api/operator/</c>,
/// and <c>index.html</c>'s <c>&lt;base href&gt;</c> points there. Link through these, never a literal path.
/// </summary>
public static class AdminRoutes
{
    public const string Home = "./";
    public const string Users = "users";
    public const string Seekers = "seekers";
    public const string Workers = "workers";
    public const string Badges = "badges";
    public const string ErrorReports = "error-reports";
}
