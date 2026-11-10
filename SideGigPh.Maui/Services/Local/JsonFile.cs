using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.Logging;

namespace SideGigPh.Maui.Services.Local;

/// <summary>
/// One JSON document in app-private storage (<see cref="FileSystem.AppDataDirectory"/>).
/// Writes go to a temp file that is then moved into place, so a crash mid-write never leaves a
/// half-written file; a corrupt or unreadable file reads as missing instead of blocking launch.
/// Pass a source-generated <see cref="JsonTypeInfo{T}"/> so it keeps working with trimming/AOT.
/// </summary>
public sealed class JsonFile<T>(string fileName, JsonTypeInfo<T> typeInfo, ILogger logger) where T : class
{
    private readonly string _path = Path.Combine(FileSystem.AppDataDirectory, fileName);
    private readonly SemaphoreSlim _gate = new(1, 1);

    public async Task<T?> ReadAsync()
    {
        await _gate.WaitAsync();
        try
        {
            if (!File.Exists(_path))
            {
                return null;
            }

            await using var stream = File.OpenRead(_path);
            return await JsonSerializer.DeserializeAsync(stream, typeInfo);
        }
        catch (Exception ex) when (ex is JsonException or IOException)
        {
            logger.LogError(ex, "Could not read {Path}; starting fresh.", _path);
            return null;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task WriteAsync(T value)
    {
        await _gate.WaitAsync();
        try
        {
            var tempPath = _path + ".tmp";
            await using (var stream = File.Create(tempPath))
            {
                await JsonSerializer.SerializeAsync(stream, value, typeInfo);
            }

            File.Move(tempPath, _path, overwrite: true);
        }
        finally
        {
            _gate.Release();
        }
    }
}
