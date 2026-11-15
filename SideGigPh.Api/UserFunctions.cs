using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using SideGigPh.AzureSQL;
using SideGigPh.Contracts;

namespace SideGigPh.Api;

/// <summary>The signed-in person's own profile: <c>users/me</c>. Nobody else can read it.</summary>
public sealed class UserFunctions(
    FirebaseTokenValidator auth, UserStore users, PushDeviceStore devices, BlobContainerClient photos, IAccountDeleter accounts, CallerLimits limits)
{
    [Function("GetMe")]
    public async Task<IActionResult> Get(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "users/me")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        if (await auth.GetUidAsync(request, cancellationToken) is not { } uid)
        {
            return new UnauthorizedResult();
        }

        return await users.GetAsync(uid, cancellationToken) is { } profile ? new OkObjectResult(profile) : new NotFoundResult();
    }

    /// <summary>
    /// <c>PUT users/me</c>. The number saved is the token's verified one, never the body's, so a profile can't claim
    /// someone else's number (publishing refuses a number another account uses as a Job Seeker).
    /// </summary>
    [Function("SaveMe")]
    public async Task<IActionResult> Save(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "users/me")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        if (await auth.GetCallerAsync(request, cancellationToken) is not { } caller)
        {
            return new UnauthorizedResult();
        }

        if (!limits.TryTake(Limited.Profile, caller.Uid))
        {
            return ApiResults.TooMany();
        }

        if (await ApiResults.ReadAsync<UserDto>(request, cancellationToken) is not { } profile)
        {
            return ApiResults.BadRequest();
        }

        return new OkObjectResult(await users.SaveAsync(caller.Uid, profile with { PhoneNumber = caller.PhoneNumber }, cancellationToken));
    }

    /// <summary><c>PUT users/me/devices</c>: this phone gets the caller's new-message notifications.</summary>
    [Function("SaveMyDevice")]
    public async Task<IActionResult> SaveDevice(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "users/me/devices")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        if (await auth.GetUidAsync(request, cancellationToken) is not { } uid)
        {
            return new UnauthorizedResult();
        }

        if (await ApiResults.ReadAsync<SaveDeviceRequest>(request, cancellationToken) is not { } device)
        {
            return ApiResults.BadRequest();
        }

        return await devices.SaveAsync(uid, device, cancellationToken) ? new NoContentResult() : ApiResults.BadRequest();
    }

    [Function("DeleteMe")]
    public async Task<IActionResult> Delete(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "users/me")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        if (await auth.GetUidAsync(request, cancellationToken) is not { } uid)
        {
            return new UnauthorizedResult();
        }

        await users.DeleteAsync(uid, cancellationToken);
        return new NoContentResult();
    }

    /// <summary>
    /// <c>DELETE users/me/account</c>: Delete account (App Store 5.1.1(v), Google Play). Everything the caller has in the
    /// database (<see cref="UserStore.DeleteAccountAsync"/>), the photos they uploaded, then their sign-in account.
    /// Answers <c>204</c> when the sign-in account is gone too, or <c>202</c> when this server can't delete it (no
    /// service account) and the app must.
    /// </summary>
    [Function("DeleteMyAccount")]
    public async Task<IActionResult> DeleteAccount(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "users/me/account")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        if (await auth.GetUidAsync(request, cancellationToken) is not { } uid)
        {
            return new UnauthorizedResult();
        }

        var photoIds = await users.DeleteAccountAsync(uid, cancellationToken);
        await PhotoFunctions.DeleteOwnedAsync(photos, uid, photoIds, cancellationToken);
        return await accounts.DeleteAsync(uid, cancellationToken) ? new NoContentResult() : new AcceptedResult();
    }
}
