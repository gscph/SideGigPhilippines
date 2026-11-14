using Microsoft.Extensions.Logging;
using SideGigPh.Contracts;
using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services.Diagnostics;

/// <summary>
/// Sends the waiting error reports in the background, never in the person's way: only while someone is signed in
/// (the API needs the token; what happened before sign-in waits), with the switch on, a batch of
/// <see cref="ErrorReportRules.MaxReports"/> at a time. A failure is logged as a warning (not an error, which would
/// record a report about sending reports) and the reports wait for the next try; a batch the server refuses as bad is
/// dropped, so it can't block the ones after it.
/// </summary>
public sealed class ErrorReportSender(
    ErrorLog log,
    IErrorReportRepository repository,
    IIdentityProvider identity,
    DeviceStamp device,
    ILogger<ErrorReportSender> logger)
{
    private readonly SemaphoreSlim _sending = new(1, 1);

    /// <summary>Sends what is waiting, batch by batch. True when nothing is left; false when it stopped (signed out, offline, off).</summary>
    public async Task<bool> SendAsync()
    {
        if (!await _sending.WaitAsync(0))
        {
            return false; // already sending
        }

        try
        {
            await log.LoadAsync();
            while (log.SendReports && log.Queued is { Count: > 0 } waiting)
            {
                if (await identity.GetCurrentUserAsync() is null)
                {
                    return false;
                }

                var batch = waiting.Take(ErrorReportRules.MaxReports).ToList();
                try
                {
                    if (!await repository.SendAsync(log.InstallId, device, batch))
                    {
                        logger.LogWarning("The server refused {Count} error reports; dropped.", batch.Count);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Couldn't send {Count} error reports; they wait for the next try.", batch.Count);
                    return false;
                }

                await log.RemoveSentAsync(batch);
            }

            return true;
        }
        finally
        {
            _sending.Release();
        }
    }
}
