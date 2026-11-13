using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services.Local;

/// <summary>
/// Keeps the onboarding state as one JSON file in app-private storage (<c>onboarding.json</c>),
/// read once and then held in memory. A corrupt file starts the interview fresh.
/// </summary>
public sealed class FileOnboardingStore(ILogger<FileOnboardingStore> logger) : IOnboardingStore
{
    private readonly JsonFile<OnboardingState> _file = new("onboarding.json", OnboardingJsonContext.Default.OnboardingState, logger);
    private Task<OnboardingState>? _loading;

    public Task<OnboardingState> LoadAsync() => _loading ??= ReadAsync();

    public async Task SaveAsync(OnboardingState state)
    {
        _loading = Task.FromResult(state);
        await _file.WriteAsync(state);
    }

    public async Task ClearAsync()
    {
        _loading = Task.FromResult(new OnboardingState());
        await _file.DeleteAsync();
    }

    // Old answers may name towns we no longer serve, or a retired trade; they are dropped so the interview asks again.
    private async Task<OnboardingState> ReadAsync() => (await _file.ReadAsync() ?? new OnboardingState()).FitToCatalog();
}

/// <summary>Source-generated serializer so the store keeps working with trimming/AOT in Release builds.</summary>
[JsonSourceGenerationOptions(UseStringEnumConverter = true)]
[JsonSerializable(typeof(OnboardingState))]
internal partial class OnboardingJsonContext : JsonSerializerContext
{
}
