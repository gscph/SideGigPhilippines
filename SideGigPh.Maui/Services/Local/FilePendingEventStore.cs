using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services.Local;

/// <summary>The usage events waiting to be uploaded: one JSON file in app-private storage (<c>pending-events.json</c>).</summary>
public sealed class FilePendingEventStore(ILogger<FilePendingEventStore> logger) : IPendingEventStore
{
    private readonly JsonFile<PendingEventsState> _file = new("pending-events.json", PendingEventsJsonContext.Default.PendingEventsState, logger);

    public Task<PendingEventsState?> LoadAsync() => _file.ReadAsync();

    public Task SaveAsync(PendingEventsState state) => _file.WriteAsync(state);
}

/// <summary>Source-generated serializer for <c>pending-events.json</c>, trim-safe in Release builds.</summary>
[JsonSerializable(typeof(PendingEventsState))]
internal partial class PendingEventsJsonContext : JsonSerializerContext
{
}
