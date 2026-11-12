using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using SideGigPh.AzureSQL;
using SideGigPh.Contracts;

namespace SideGigPh.Api;

/// <summary>The signed-in person's own profile: <c>users/me</c>. Nobody else can read it.</summary>
public sealed class UserFunctions(FirebaseTokenValidator auth, UserStore users)
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

    [Function("SaveMe")]
    public async Task<IActionResult> Save(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "users/me")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        if (await auth.GetUidAsync(request, cancellationToken) is not { } uid)
        {
            return new UnauthorizedResult();
        }

        if (await ApiResults.ReadAsync<UserDto>(request, cancellationToken) is not { } profile)
        {
            return ApiResults.BadRequest();
        }

        return new OkObjectResult(await users.SaveAsync(uid, profile, cancellationToken));
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
}
