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
public sealed class ProfessionalFunctions(
    FirebaseTokenValidator auth, ProfessionalStore professionals, SafetyStore safety, CallerLimits limits, ILogger<ProfessionalFunctions> logger)
{
    /// <summary>
    /// <c>GET search/professionals?categoryId=aircon&amp;areaId=rizal&amp;serviceId=…&amp;cityId=…&amp;budgetId=…&amp;otherText=…&amp;skip=0&amp;take=4</c>:
    /// one page of the trade's workers ranked for this Seeker (only <c>categoryId</c> is required; <c>take</c> defaults
    /// to <see cref="Paging.Search"/>; <c>otherText</c> is what they typed under "Others").
    /// </summary>
    [Function("SearchProfessionalsRanked")]
    public async Task<IActionResult> SearchRanked(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "search/professionals")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        if (await auth.GetUidAsync(request, cancellationToken) is not { } uid)
        {
            return new UnauthorizedResult();
        }

        var query = request.Query;
        string? categoryId = query["categoryId"];
        if (string.IsNullOrEmpty(categoryId))
        {
            return ApiResults.BadRequest("categoryId-required");
        }

        if (Number(query["skip"], 0) is not { } skip || skip < 0)
        {
            return ApiResults.BadRequest(ApiErrors.Invalid("skip"));
        }

        if (Number(query["take"], Paging.Search) is not { } take || take < 1)
        {
            return ApiResults.BadRequest(ApiErrors.Invalid("take"));
        }

        var rank = new RankRequest(Optional(query["serviceId"]), Optional(query["cityId"]), Optional(query["budgetId"]),
            Optional(query["otherText"]) is { } typed && typed.Length > OtherWork.MaxText ? typed[..OtherWork.MaxText] : Optional(query["otherText"]));
        var hidden = await safety.HiddenForAsync(uid, cancellationToken);
        return new OkObjectResult(await professionals.SearchPageAsync(categoryId, Optional(query["areaId"]), rank, skip, take, hidden, cancellationToken));
    }

    private static string? Optional(string? value) => string.IsNullOrEmpty(value) ? null : value;

    /// <summary>A whole-number query value, <paramref name="missing"/> when absent, or null when it isn't a number.</summary>
    private static int? Number(string? value, int missing) =>
        string.IsNullOrEmpty(value) ? missing : int.TryParse(value, out var number) ? number : null;

    /// <summary><c>GET professionals?categoryId=aircon&amp;areaId=rizal</c> (areaId optional), unranked: for app versions before paging.</summary>
    [Function("SearchProfessionals")]
    public async Task<IActionResult> Search(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "professionals")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        if (await auth.GetUidAsync(request, cancellationToken) is not { } uid)
        {
            return new UnauthorizedResult();
        }

        string? categoryId = request.Query["categoryId"];
        string? areaId = request.Query["areaId"];
        if (string.IsNullOrEmpty(categoryId))
        {
            return ApiResults.BadRequest("categoryId-required");
        }

        var hidden = await safety.HiddenForAsync(uid, cancellationToken);
        return new OkObjectResult(await professionals.SearchAsync(categoryId, string.IsNullOrEmpty(areaId) ? null : areaId, hidden, cancellationToken));
    }

    [Function("GetProfessional")]
    public async Task<IActionResult> Get(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "professionals/{id}")] HttpRequest request,
        string id,
        CancellationToken cancellationToken)
    {
        if (await auth.GetUidAsync(request, cancellationToken) is not { } uid)
        {
            return new UnauthorizedResult();
        }

        // Someone either of them blocked has no profile for the caller.
        if (id != uid && (await safety.HiddenForAsync(uid, cancellationToken)).Contains(id))
        {
            return new NotFoundResult();
        }

        return await professionals.GetAsync(id, cancellationToken, withNumber: id == uid) is { } listing ? new OkObjectResult(listing) : new NotFoundResult();
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

        if (!limits.TryTake(Limited.Listing, caller.Uid))
        {
            return ApiResults.TooMany();
        }

        if (await ApiResults.ReadAsync<PublishListingRequest>(request, cancellationToken) is not { } listing)
        {
            return ApiResults.BadRequest(ApiErrors.BadBody, "The body was empty (null).");
        }

        if (ProfessionalStore.InvalidField(listing, caller.PhoneNumber) is { } field)
        {
            var sent = JsonSerializer.Serialize(listing, ApiJson.Default.PublishListingRequest);
            // Only the field: the listing holds the references' names and numbers, which never go into the logs.
            logger.LogWarning("Publish by {Uid}: invalid {Field}.", caller.Uid, field);
            return ApiResults.BadRequest(ApiErrors.Invalid(field),
                $"{field} is missing or not valid (categoryId, areaId, at least one serviceIds and cityIds, a profilePhotoId, "
                + $"at least {ProfessionalStore.MinWorkPhotos} workPhotoIds and {ProfessionalStore.MinReferences}-{ProfessionalStore.MaxReferences} "
                + $"references with a name and a distinct +639 number that isn't the worker's own are required). Received: {sent}");
        }

        return await professionals.PublishAsync(caller.Uid, caller.PhoneNumber, listing, cancellationToken) switch
        {
            PublishOutcome.Published => new OkObjectResult(await professionals.GetAsync(caller.Uid, cancellationToken, withNumber: true)),
            PublishOutcome.PhoneTaken => ApiResults.Conflict(ApiErrors.PhoneTaken),
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

        if (!limits.TryTake(Limited.Listing, uid))
        {
            return ApiResults.TooMany();
        }

        await professionals.RemoveAsync(uid, cancellationToken);
        return new NoContentResult();
    }
}
