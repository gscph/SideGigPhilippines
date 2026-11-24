using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services;

/// <summary>Where the phone's usage events go (the server's <c>app_events</c>, <c>POST events</c>).</summary>
public interface IAppEventRepository
{
    /// <summary>
    /// Uploads up to <c>AppEventRules.MaxBatch</c> events. True when the server kept them, false when it refused them as
    /// bad (sending them again won't help); throws when they didn't get there (offline, server down, too many), so they
    /// wait. Never logs an error itself: an upload that fails offline is not a problem to report.
    /// </summary>
    Task<bool> SendAsync(IReadOnlyList<PendingEvent> events);
}
