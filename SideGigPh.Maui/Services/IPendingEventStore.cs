using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services;

/// <summary>Where the phone keeps its usage events until they are uploaded (one JSON file, <c>FilePendingEventStore</c>).</summary>
public interface IPendingEventStore
{
    /// <summary>The saved events, or null when there are none (or the file can't be read).</summary>
    Task<PendingEventsState?> LoadAsync();

    Task SaveAsync(PendingEventsState state);
}
