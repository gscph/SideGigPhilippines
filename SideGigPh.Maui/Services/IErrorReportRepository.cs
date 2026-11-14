using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services;

/// <summary>Where the app's error reports go (the server's <c>ErrorReports</c>, diagnostics only).</summary>
public interface IErrorReportRepository
{
    /// <summary>
    /// Sends up to <c>ErrorReportRules.MaxReports</c> reports from this install. True when the server kept them, false
    /// when it refused them as bad (sending them again won't help); throws when they didn't get there (offline, server
    /// down, too many), so they wait. Never logs an error itself: that would record a report about sending reports.
    /// </summary>
    Task<bool> SendAsync(string installId, DeviceStamp device, IReadOnlyList<ErrorReport> reports);
}
