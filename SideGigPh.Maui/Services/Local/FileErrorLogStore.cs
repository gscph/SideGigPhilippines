using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging.Abstractions;
using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services.Local;

/// <summary>
/// The error log on the phone: one JSON file in app-private storage (<c>diagnostics.json</c>). Start over keeps it
/// (it is about the app, not the person). It logs nothing: its logger would feed the error log it stores.
/// </summary>
public sealed class FileErrorLogStore : IErrorLogStore
{
    private readonly JsonFile<DiagnosticsState> _file = new("diagnostics.json", DiagnosticsJsonContext.Default.DiagnosticsState, NullLogger.Instance);

    public Task<DiagnosticsState?> LoadAsync() => _file.ReadAsync();

    public Task SaveAsync(DiagnosticsState state) => _file.WriteAsync(state);
}

/// <summary>Source-generated serializer for <c>diagnostics.json</c>, trim-safe in Release builds.</summary>
[JsonSourceGenerationOptions(UseStringEnumConverter = true)]
[JsonSerializable(typeof(DiagnosticsState))]
internal partial class DiagnosticsJsonContext : JsonSerializerContext
{
}
