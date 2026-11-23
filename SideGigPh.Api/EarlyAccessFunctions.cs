using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using SideGigPh.AzureSQL;
using SideGigPh.Contracts;

namespace SideGigPh.Api;

/// <summary>
/// The landing page's Early Access form (SideGigPh.FrontDoor, a Static Web App on tarbaho.com). Public: no sign-in.
/// Each sign-up is kept in Azure SQL (<see cref="EarlyAccessStore"/>, one row per email). Limited per visitor's
/// IP address and in total, and a hidden field bots fill in is answered as done with nothing sent.
/// </summary>
public sealed class EarlyAccessFunctions(EarlyAccessStore store, CallerLimits limits, TimeProvider clock)
{
    /// <summary>
    /// <c>POST early-access</c> (<see cref="EarlyAccessRequest"/>): 204 when it is kept (the same address again too); 400
    /// <c>invalid-email</c> or <c>invalid-phone</c>; 429 past the limits.
    /// </summary>
    [Function("JoinEarlyAccess")]
    public async Task<IActionResult> Join(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "early-access")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        if (await ApiResults.ReadAsync<EarlyAccessRequest>(request, cancellationToken) is not { } body)
        {
            return ApiResults.BadRequest(ApiErrors.BadBody);
        }

        if (!EarlyAccessRules.IsValidEmail(body.Email))
        {
            return ApiResults.BadRequest(ApiErrors.Invalid("email"));
        }

        if (EarlyAccessRules.CleanPhone(body.Phone) is not { } phone)
        {
            return ApiResults.BadRequest(ApiErrors.Invalid("phone"));
        }

        if (!string.IsNullOrEmpty(body.Website))
        {
            return new NoContentResult();  // a bot: looks done, nothing sent
        }

        if (!limits.TryTake(Limited.EarlyAccess, ClientAddress.Of(request)) || !limits.TryTake(Limited.EarlyAccessTotal, "all"))
        {
            return ApiResults.TooMany();
        }

        var signup = new EarlyAccessSignup(
            EarlyAccessRules.Normalize(body.Email), phone, EarlyAccessRules.CleanRole(body.Role), EarlyAccessRules.CleanLanguage(body.Language));
        await store.SaveAsync(signup, clock.GetUtcNow(), cancellationToken);
        return new NoContentResult();
    }
}
