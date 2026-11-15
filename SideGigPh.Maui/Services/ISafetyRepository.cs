using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services;

/// <summary>
/// Reports and blocks (App Store 1.2, Google Play user-generated content). A report goes to the operator, who answers
/// within 5 business days; a block works at once on the server: neither person sees the other in search, on a
/// profile or in the Inbox, and no new message goes through.
/// </summary>
public interface ISafetyRepository
{
    /// <summary>Sends the report (and the block when it asks for one). Throws when it didn't send.</summary>
    Task ReportAsync(UserReport report);

    /// <summary>Blocks the person. Throws when it didn't send.</summary>
    Task BlockAsync(string personId);
}
