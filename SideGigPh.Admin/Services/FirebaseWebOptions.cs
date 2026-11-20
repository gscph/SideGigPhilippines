namespace SideGigPh.Admin.Services;

/// <summary>
/// The Firebase web app's settings (section <c>Firebase</c> of <c>wwwroot/appsettings.json</c>): Firebase console →
/// Project settings → Your apps → the Web app. Not secrets (every web page that uses Firebase ships them); the API
/// trusts only the ID token it checks, and only the uids in its <c>AdminUids</c> setting get past <c>admin/…</c>.
/// </summary>
public sealed class FirebaseWebOptions
{
    public string ApiKey { get; set; } = "";
    public string AuthDomain { get; set; } = "";
    public string ProjectId { get; set; } = "";
    public string AppId { get; set; } = "";

    public bool IsComplete =>
        !string.IsNullOrWhiteSpace(ApiKey) && !string.IsNullOrWhiteSpace(AuthDomain)
        && !string.IsNullOrWhiteSpace(ProjectId) && !string.IsNullOrWhiteSpace(AppId);
}
