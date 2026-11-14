namespace SideGigPh.Maui.Models;

/// <summary>What an <see cref="ErrorReport"/> describes.</summary>
public enum ErrorKind
{
    /// <summary>The app logged an error and carried on.</summary>
    Error,

    /// <summary>An exception nothing caught: the app closed.</summary>
    Crash,

    /// <summary>The loading screen stayed up too long (looks frozen).</summary>
    Slow,
}

/// <summary>
/// One problem the app had, already scrubbed of personal data, waiting on the phone to be sent. The same problem
/// repeated is one report with a higher <see cref="Count"/> and a later <see cref="LastAt"/>.
/// </summary>
/// <param name="Source">Where in the app (the logger's category).</param>
/// <param name="Detail">The exception and its stack trace.</param>
/// <param name="Route">The screen it happened on.</param>
/// <param name="Trail">The screens and steps just before it, oldest first, one per line.</param>
public sealed record ErrorReport(
    ErrorKind Kind,
    string Source,
    string Message,
    string? Detail,
    string? Route,
    string? Trail,
    int Count,
    DateTimeOffset FirstAt,
    DateTimeOffset LastAt);

/// <summary>What the phone runs, sent with every batch: never the person.</summary>
/// <param name="AppVersion">The store version and build, e.g. <c>1.0 (12)</c>.</param>
/// <param name="CodeVersion">The git commit the app was built from, e.g. <c>48466c9</c> (<c>-dirty</c> with uncommitted changes).</param>
/// <param name="Channel"><c>dev</c>, <c>beta</c> or <c>prod</c> (the csproj's <c>AppChannel</c>).</param>
/// <param name="Platform">The OS and its version, e.g. <c>android 14</c>.</param>
/// <param name="Device">The maker and model, e.g. <c>samsung SM-A155F</c>.</param>
public sealed record DeviceStamp(string AppVersion, string CodeVersion, string Channel, string Platform, string? Device);

/// <summary>The error log kept on the phone (<c>diagnostics.json</c>): the switch, the install id and the waiting reports.</summary>
public sealed class DiagnosticsState
{
    /// <summary>Random per install, so reports from one phone can be grouped without knowing whose phone it is.</summary>
    public string? InstallId { get; set; }

    /// <summary>The Me page's "Send error reports" switch; on unless the person turned it off.</summary>
    public bool SendReports { get; set; } = true;

    public List<ErrorReport> Reports { get; set; } = [];
}
