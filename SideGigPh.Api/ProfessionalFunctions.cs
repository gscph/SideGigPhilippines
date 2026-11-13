using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SideGigPh.AzureSQL;
using SideGigPh.Contracts;

namespace SideGigPh.Api;

/// <summary>
/// Skilled Worker listings. Any signed-in user can search and read them; only the worker publishes or removes
/// their own (<c>professionals/me</c>, id = their uid).
/// </summary>
public sealed class ProfessionalFunctions(FirebaseTokenValidator auth, ProfessionalStore professionals, ILogger<ProfessionalFunctions> logger)
{
    /// <summary><c>GET professionals?categoryId=aircon&amp;areaId=rizal</c> (areaId optional).</summary>
    [Function("SearchProfessionals")]
    public async Task<IActionResult> Search(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "professionals")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        if (await auth.GetUidAsync(request, cancellationToken) is null)
        {
            return new UnauthorizedResult();
        }

        string? categoryId = request.Query["categoryId"];
        string? areaId = request.Query["areaId"];
        if (string.IsNullOrEmpty(categoryId))
        {
            return ApiResults.BadRequest("categoryId-required");
        }

        return new OkObjectResult(await professionals.SearchAsync(categoryId, string.IsNullOrEmpty(areaId) ? null : areaId, cancellationToken));
    }

    [Function("GetProfessional")]
    public async Task<IActionResult> Get(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "professionals/{id}")] HttpRequest request,
        string id,
        CancellationToken cancellationToken)
    {
        if (await auth.GetUidAsync(request, cancellationToken) is null)
        {
            return new UnauthorizedResult();
        }

        return await professionals.GetAsync(id, cancellationToken) is { } listing ? new OkObjectResult(listing) : new NotFoundResult();
    }

    /// <summary>Publishes the caller's listing, or replaces it. Needs a verified phone number on the account.</summary>
    [Function("PublishProfessional")]
    public async Task<IActionResult> Publish(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "professionals/me")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        if (await auth.GetCallerAsync(request, cancellationToken) is not { } caller)
        {
            return new UnauthorizedResult();
        }

        if (caller.PhoneNumber is null)
        {
            return ApiResults.Forbidden(ApiErrors.PhoneRequired);
        }

        if (await ApiResults.ReadAsync<PublishListingRequest>(request, cancellationToken) is not { } listing)
        {
            return ApiResults.BadRequest(ApiErrors.BadBody, "The body was empty (null).");
        }

        if (ProfessionalStore.InvalidField(listing, caller.PhoneNumber) is { } field)
        {
            var sent = JsonSerializer.Serialize(listing, ApiJson.Default.PublishListingRequest);
            logger.LogWarning("Publish by {Uid}: invalid {Field} in {Listing}.", caller.Uid, field, sent);
            return ApiResults.BadRequest(ApiErrors.Invalid(field),
                $"{field} is missing or not valid (categoryId, areaId, at least one serviceIds and cityIds, a profilePhotoId, "
                + $"at least {ProfessionalStore.MinWorkPhotos} workPhotoIds and {ProfessionalStore.MinReferences}-{ProfessionalStore.MaxReferences} "
                + $"references with a name and a distinct +639 number that isn't the worker's own are required). Received: {sent}");
        }

        return await professionals.PublishAsync(caller.Uid, caller.PhoneNumber, listing, cancellationToken) switch
        {
            PublishOutcome.Published => new OkObjectResult(await professionals.GetAsync(caller.Uid, cancellationToken)),
            PublishOutcome.PhoneTaken => ApiResults.Conflict(ApiErrors.PhoneTaken),
            PublishOutcome.PhoneUsedBySeeker => ApiResults.Conflict(ApiErrors.PhoneUsedBySeeker),
            PublishOutcome.PhotoInUse => ApiResults.Conflict(ApiErrors.PhotoInUse),
            _ => ApiResults.BadRequest(),
        };
    }

    [Function("RemoveProfessional")]
    public async Task<IActionResult> Remove(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "professionals/me")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        if (await auth.GetUidAsync(request, cancellationToken) is not { } uid)
        {
            return new UnauthorizedResult();
        }

        await professionals.RemoveAsync(uid, cancellationToken);
        return new NoContentResult();
    }
}
