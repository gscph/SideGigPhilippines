using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services.Local;

/// <summary>
/// Keeps the onboarding state as one JSON file in app-private storage
/// (<see cref="FileSystem.AppDataDirectory"/>). Writes go to a temp file first and are then
/// moved into place, so a crash mid-write never leaves a half-written file.
/// </summary>
public sealed class FileOnboardingStore(ILogger<FileOnboardingStore> logger) : IOnboardingStore
{
    private readonly string _path = Path.Combine(FileSystem.AppDataDirectory, "onboarding.json");
    private readonly SemaphoreSlim _gate = new(1, 1);
    private OnboardingState? _state;

    public async Task<OnboardingState> LoadAsync()
    {
        if (_state is not null)
        {
            return _state;
        }

        await _gate.WaitAsync();
        try
        {
            return _state ??= await ReadAsync() ?? new OnboardingState();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SaveAsync(OnboardingState state)
    {
        await _gate.WaitAsync();
        try
        {
            _state = state;
            var tempPath = _path + ".tmp";
            await using (var stream = File.Create(tempPath))
            {
                await JsonSerializer.SerializeAsync(stream, state, OnboardingJsonContext.Default.OnboardingState);
            }

            File.Move(tempPath, _path, overwrite: true);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<OnboardingState?> ReadAsync()
    {
        if (!File.Exists(_path))
        {
            return null;
        }

        try
        {
            await using var stream = File.OpenRead(_path);
            return await JsonSerializer.DeserializeAsync(stream, OnboardingJsonContext.Default.OnboardingState);
        }
        catch (Exception ex) when (ex is JsonException or IOException)
        {
            // A corrupt or unreadable file must not block launch: start the interview fresh.
            logger.LogError(ex, "Could not read saved onboarding state at {Path}; starting fresh.", _path);
            return null;
        }
    }
}

/// <summary>Source-generated serializer so the store keeps working with trimming/AOT in Release builds.</summary>
[JsonSourceGenerationOptions(UseStringEnumConverter = true)]
[JsonSerializable(typeof(OnboardingState))]
internal partial class OnboardingJsonContext : JsonSerializerContext
{
}
