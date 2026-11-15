using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using SideGigPh.AzureSQL;

namespace SideGigPh.Api;

/// <summary>Wakes a cold Function App and a paused database before the person's first real call.</summary>
public sealed class WarmupFunctions(FirebaseTokenValidator auth, WarmupStore warmup)
{
    /// <summary>
    /// <c>GET warmup</c>: the app calls it when it opens and when it comes back after a while (<c>ServerWarmup</c>).
    /// Checking the token also loads Google's signing keys. Signed-in only, so nobody can keep the database awake
    /// (and billed) from outside; each instance wakes the database at most once per <see cref="WarmupStore.AwakeFor"/>.
    /// </summary>
    [Function("Warmup")]
    public async Task<IActionResult> Warmup(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "warmup")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        if (await auth.GetUidAsync(request, cancellationToken) is null)
        {
            return new UnauthorizedResult();
        }

        await warmup.WakeAsync(cancellationToken);
        return new NoContentResult();
    }
}
