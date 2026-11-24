using Microsoft.Extensions.Logging;
using SideGigPh.Contracts;

namespace SideGigPh.Maui.Services.Analytics;

/// <summary>
/// Uploads the usage events waiting on the phone (<see cref="TarbahoAnalytics"/>) in the background, never in the
/// person's way: <see cref="FirstTry"/> after launch, every <see cref="Every"/> while the app is open, and when it goes
/// to the background (<see cref="AppHidden"/>, while the system still gives the app a few seconds). Only while someone
/// is signed in (the API needs the token). The <see cref="AppEventRules.MaxBatch"/> oldest go at a time and leave the
/// phone only once the server kept them; a failure stops until the next try and deletes nothing (mobile data counts
/// as connected: many people have nothing else). A batch the server refuses as bad is dropped, so it can't block the rest.
/// </summary>
public sealed class AnalyticsUploader(
    TarbahoAnalytics analytics,
    IAppEventRepository repository,
    IIdentityProvider identity,
    AppVisibility visibility,
    ILogger<AnalyticsUploader> logger)
{
    /// <summary>The first upload after launch: late enough not to compete with the app's own start-up calls.</summary>
    public static readonly TimeSpan FirstTry = TimeSpan.FromSeconds(45);

    /// <summary>How often waiting events are uploaded while the app is open.</summary>
    public static readonly TimeSpan Every = TimeSpan.FromMinutes(15);

    private readonly SemaphoreSlim _sending = new(1, 1);
    private int _started;

    /// <summary>Starts the background uploads (once, from <c>App</c>).</summary>
    public void Start()
    {
        if (Interlocked.Exchange(ref _started, 1) == 0)
        {
            _ = UploadInTheBackgroundAsync();
        }
    }

    /// <summary>The app went to the background: a new session may start when it's back, and what is waiting goes now.</summary>
    public void AppHidden()
    {
        analytics.AppHidden();
        _ = SendQuietlyAsync();
    }

    /// <summary>The app is on screen again.</summary>
    public void AppShown() => analytics.AppShown();

    /// <summary>Uploads what is waiting, batch by batch. True when nothing is left; false when it stopped (signed out, offline).</summary>
    public async Task<bool> SendAsync()
    {
        if (!await _sending.WaitAsync(0))
        {
            return false; // already uploading
        }

        try
        {
            while (await analytics.OldestAsync(AppEventRules.MaxBatch) is { Count: > 0 } batch)
            {
                if (await identity.GetCurrentUserAsync() is null)
                {
                    return false;
                }

                try
                {
                    if (!await repository.SendAsync(batch))
                    {
                        logger.LogWarning("The server refused {Count} usage events; dropped.", batch.Count);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Couldn't upload {Count} usage events; they wait for the next try.", batch.Count);
                    return false;
                }

                await analytics.RemoveAsync(batch);
            }

            return true;
        }
        finally
        {
            _sending.Release();
        }
    }

    private async Task<bool> SendQuietlyAsync()
    {
        try
        {
            return await SendAsync();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Couldn't upload the usage events.");
            return false; // never take the app down over its counts
        }
    }

    private async Task UploadInTheBackgroundAsync()
    {
        try
        {
            await Task.Delay(FirstTry);
            using var timer = new PeriodicTimer(Every);
            do
            {
                if (visibility.IsVisible)
                {
                    await SendQuietlyAsync();
                }
            }
            while (await timer.WaitForNextTickAsync());
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "The usage-event uploads stopped.");
        }
    }
}
