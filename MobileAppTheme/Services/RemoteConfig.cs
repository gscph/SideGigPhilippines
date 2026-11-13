using Microsoft.Extensions.Logging;

namespace MobileAppTheme.Services;

/// <summary>A value saved with the time it was last checked, and the server's version of it (e.g. an ETag).</summary>
public sealed record Stamped<T>(T Value, DateTimeOffset SavedAt, string? Version = null);

/// <summary>
/// What the server answered: a new <see cref="Value"/> with its <see cref="Version"/>, or (<see cref="IsUnchanged"/>)
/// nothing to download because the phone already has that version.
/// </summary>
public sealed record ConfigFetch<T>(T? Value, string? Version) where T : class
{
    public bool IsUnchanged => Value is null;
}

/// <summary>Fetches the app's config from its server.</summary>
public interface IConfigSource<T> where T : class
{
    /// <summary>
    /// The server's config, or "unchanged" when it is still <paramref name="savedVersion"/> (no download). Null when
    /// the server can't be reached.
    /// </summary>
    Task<ConfigFetch<T>?> FetchAsync(string? savedVersion, CancellationToken cancellationToken);
}

/// <summary>Where <see cref="RemoteConfig{T}"/> keeps its last copy on the phone (default: <c>Local.FileConfigCache</c>).</summary>
public interface IConfigCache<T> where T : class
{
    /// <summary>The saved copy, or null when there is none or it can't be read.</summary>
    Task<Stamped<T>?> ReadAsync();

    Task WriteAsync(Stamped<T> saved);
}

/// <summary>How a <see cref="RemoteConfig{T}"/> decides.</summary>
/// <param name="IsUsable">A copy the app can run on (e.g. no empty list). Anything else is never used or saved.</param>
/// <param name="FreshFor">A saved copy checked less than this long ago is used with no network call.</param>
/// <param name="FetchTimeout">The longest the app waits for the server when it opens; after that it uses what it has.</param>
public sealed record RemoteConfigRules<T>(Func<T, bool> IsUsable, TimeSpan FreshFor, TimeSpan FetchTimeout)
{
    public static readonly TimeSpan DefaultFreshFor = TimeSpan.FromHours(12);

    public static readonly TimeSpan DefaultFetchTimeout = TimeSpan.FromSeconds(8);

    public RemoteConfigRules(Func<T, bool> isUsable)
        : this(isUsable, DefaultFreshFor, DefaultFetchTimeout)
    {
    }
}

/// <summary>
/// Data the app needs on every page but that may be a few hours stale (answer lists, labels, settings), loaded
/// <b>once</b> when the app opens (behind its splash) and then held in memory: pages never fetch it.
/// <list type="bullet">
/// <item>A saved copy checked less than <see cref="RemoteConfigRules{T}.FreshFor"/> ago is used with no network call.</item>
/// <item>Otherwise the server is asked, with the saved version: when it hasn't changed, nothing is downloaded and the
/// saved copy counts as checked again; when it has, the new copy is used and replaces the saved one.</item>
/// <item>When the server can't be reached in <see cref="RemoteConfigRules{T}.FetchTimeout"/>, the old saved copy is
/// still better than nothing.</item>
/// </list>
/// <see cref="LoadAsync"/> returns null only when there is no usable copy at all; the app then runs on its built-in copy.
/// </summary>
public sealed class RemoteConfig<T>(
    IConfigSource<T> source,
    IConfigCache<T> cache,
    RemoteConfigRules<T> rules,
    TimeProvider clock,
    ILogger<RemoteConfig<T>> logger) where T : class
{
    private Task<T?>? _loading;

    /// <summary>The config, loaded once per app run (later calls get the same result).</summary>
    public Task<T?> LoadAsync() => _loading ??= LoadOnceAsync();

    private async Task<T?> LoadOnceAsync()
    {
        var saved = await ReadSavedAsync();
        if (saved is not null && clock.GetUtcNow() - saved.SavedAt < rules.FreshFor)
        {
            return saved.Value;
        }

        switch (await FetchAsync(saved?.Version))
        {
            case { IsUnchanged: true } unchanged when saved is not null:
                await SaveAsync(saved with { SavedAt = clock.GetUtcNow(), Version = unchanged.Version ?? saved.Version });
                return saved.Value;

            case { Value: { } fresh } fetched when rules.IsUsable(fresh):
                await SaveAsync(new Stamped<T>(fresh, clock.GetUtcNow(), fetched.Version));
                return fresh;

            case { IsUnchanged: false }:
                logger.LogWarning("The server's {Config} is unusable; keeping what the phone has.", typeof(T).Name);
                return saved?.Value;

            default:
                return saved?.Value;
        }
    }

    private async Task<Stamped<T>?> ReadSavedAsync()
    {
        try
        {
            return await cache.ReadAsync() is { } saved && rules.IsUsable(saved.Value) ? saved : null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Couldn't read the saved {Config}.", typeof(T).Name);
            return null;
        }
    }

    private async Task<ConfigFetch<T>?> FetchAsync(string? savedVersion)
    {
        using var timeout = new CancellationTokenSource(rules.FetchTimeout, clock);
        try
        {
            return await source.FetchAsync(savedVersion, timeout.Token).WaitAsync(timeout.Token);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Couldn't fetch the {Config}; keeping what the phone has.", typeof(T).Name);
            return null;
        }
    }

    private async Task SaveAsync(Stamped<T> saved)
    {
        try
        {
            await cache.WriteAsync(saved);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Couldn't save the {Config} on the phone.", typeof(T).Name);
        }
    }
}
