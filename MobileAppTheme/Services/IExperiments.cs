namespace MobileAppTheme.Services;

/// <summary>
/// A/B tests and remote switches: the server decides which variant of something this install sees (a button's
/// wording, a step's order), and the provider reports which variant did better. A provider (Firebase Remote Config
/// with A/B Testing…) is an adapter in its own project that replaces <see cref="NoExperiments"/>; until an app adds
/// one, every question answers its fallback, so the app behaves exactly as written.
/// Keys are short snake_case words the app and the provider's console agree on.
/// </summary>
public interface IExperiments
{
    /// <summary>True when a provider is answering; false for <see cref="NoExperiments"/> or a provider that couldn't start.</summary>
    bool IsOn { get; }

    /// <summary>
    /// Gets the latest variants from the server and starts using them. Never throws: offline or failing, the last
    /// known variants (or the fallbacks) stay. Call once per launch, without making the person wait for it.
    /// </summary>
    Task RefreshAsync();

    /// <summary>The variant for <paramref name="key"/>, or <paramref name="fallback"/> when the server has none.</summary>
    string Variant(string key, string fallback);

    /// <summary>A switch: true/false for <paramref name="key"/>, or <paramref name="fallback"/> when the server has none.</summary>
    bool IsEnabled(string key, bool fallback);
}

/// <summary>The default: every answer is the fallback, until the app adds a provider (e.g. <c>AddFirebaseExperiments</c>).</summary>
public sealed class NoExperiments : IExperiments
{
    public bool IsOn => false;

    public Task RefreshAsync() => Task.CompletedTask;

    public string Variant(string key, string fallback) => fallback;

    public bool IsEnabled(string key, bool fallback) => fallback;
}
