using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.Logging;

namespace MobileAppTheme.Services.Local;

/// <summary>
/// <see cref="IConfigCache{T}"/> as one JSON file in app-private storage (<see cref="JsonFile{T}"/>: atomic write,
/// a corrupt file reads as missing). One small document read whole once per launch, so no database is needed.
/// </summary>
public sealed class FileConfigCache<T>(string fileName, JsonTypeInfo<Stamped<T>> typeInfo, ILogger<FileConfigCache<T>> logger)
    : IConfigCache<T> where T : class
{
    private readonly JsonFile<Stamped<T>> _file = new(fileName, typeInfo, logger);

    public Task<Stamped<T>?> ReadAsync() => _file.ReadAsync();

    public Task WriteAsync(Stamped<T> saved) => _file.WriteAsync(saved);
}
