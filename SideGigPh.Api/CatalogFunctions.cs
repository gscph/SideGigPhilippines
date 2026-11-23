using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using SideGigPh.AzureSQL;
using SideGigPh.Contracts;

namespace SideGigPh.Api;

/// <summary>
/// The operator site's Catalog page: the trades and jobs the app offers, renamed, added, retired and restored by an
/// operator (<see cref="CatalogStore"/>). Only the uids in <c>AdminUids</c>; anyone else gets 401 or 403 <c>not-admin</c>.
/// Every change answers with the whole catalog, so the page shows what was saved. Nothing is ever deleted (a
/// <c>DELETE</c> retires), and posts and chats keep the names they were made with.
/// </summary>
public sealed class CatalogFunctions(FirebaseTokenValidator auth, AdminList admins, CatalogStore store)
{
    /// <summary><c>GET ops/catalog</c>: every trade (retired ones too) with its jobs.</summary>
    [Function("AdminCatalog")]
    public async Task<IActionResult> Catalog(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "ops/catalog")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        var (admin, refusal) = await admins.CheckAsync(auth, request, cancellationToken);
        return admin is null ? refusal! : new OkObjectResult(await store.ReadAsync(cancellationToken));
    }

    /// <summary><c>POST ops/catalog/trades</c> (<see cref="SaveTradeRequest"/>): a new trade, last, with its "Others" job.</summary>
    [Function("AdminAddTrade")]
    public Task<IActionResult> AddTrade(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "ops/catalog/trades")] HttpRequest request,
        CancellationToken cancellationToken) =>
        WithBodyAsync<SaveTradeRequest>(request, (admin, body) => store.AddTradeAsync(admin, body, cancellationToken), cancellationToken);

    /// <summary><c>PUT ops/catalog/trades/{id}</c> (<see cref="SaveTradeRequest"/>): renames a trade.</summary>
    [Function("AdminUpdateTrade")]
    public Task<IActionResult> UpdateTrade(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "ops/catalog/trades/{id}")] HttpRequest request,
        string id,
        CancellationToken cancellationToken) =>
        WithBodyAsync<SaveTradeRequest>(request, (admin, body) => store.UpdateTradeAsync(admin, id, body, cancellationToken), cancellationToken);

    /// <summary><c>DELETE ops/catalog/trades/{id}</c>: retires a trade (never deletes it).</summary>
    [Function("AdminRetireTrade")]
    public Task<IActionResult> RetireTrade(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "ops/catalog/trades/{id}")] HttpRequest request,
        string id,
        CancellationToken cancellationToken) =>
        RunAsync(request, admin => store.SetTradeActiveAsync(admin, id, active: false, cancellationToken), cancellationToken);

    /// <summary><c>POST ops/catalog/trades/{id}/restore</c>: offers a retired trade again.</summary>
    [Function("AdminRestoreTrade")]
    public Task<IActionResult> RestoreTrade(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "ops/catalog/trades/{id}/restore")] HttpRequest request,
        string id,
        CancellationToken cancellationToken) =>
        RunAsync(request, admin => store.SetTradeActiveAsync(admin, id, active: true, cancellationToken), cancellationToken);

    /// <summary><c>POST ops/catalog/services</c> (<see cref="SaveServiceRequest"/>): a new job in a trade, just before its "Others".</summary>
    [Function("AdminAddService")]
    public Task<IActionResult> AddService(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "ops/catalog/services")] HttpRequest request,
        CancellationToken cancellationToken) =>
        WithBodyAsync<SaveServiceRequest>(request, (admin, body) => store.AddServiceAsync(admin, body, cancellationToken), cancellationToken);

    /// <summary><c>PUT ops/catalog/services/{id}</c> (<see cref="SaveServiceRequest"/>): renames a job and sets its price guide.</summary>
    [Function("AdminUpdateService")]
    public Task<IActionResult> UpdateService(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "ops/catalog/services/{id}")] HttpRequest request,
        string id,
        CancellationToken cancellationToken) =>
        WithBodyAsync<SaveServiceRequest>(request, (admin, body) => store.UpdateServiceAsync(admin, id, body, cancellationToken), cancellationToken);

    /// <summary><c>DELETE ops/catalog/services/{id}</c>: retires a job (never deletes it; never a trade's "Others").</summary>
    [Function("AdminRetireService")]
    public Task<IActionResult> RetireService(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "ops/catalog/services/{id}")] HttpRequest request,
        string id,
        CancellationToken cancellationToken) =>
        RunAsync(request, admin => store.SetServiceActiveAsync(admin, id, active: false, cancellationToken), cancellationToken);

    /// <summary><c>POST ops/catalog/services/{id}/restore</c>: offers a retired job again.</summary>
    [Function("AdminRestoreService")]
    public Task<IActionResult> RestoreService(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "ops/catalog/services/{id}/restore")] HttpRequest request,
        string id,
        CancellationToken cancellationToken) =>
        RunAsync(request, admin => store.SetServiceActiveAsync(admin, id, active: true, cancellationToken), cancellationToken);

    private async Task<IActionResult> WithBodyAsync<T>(
        HttpRequest request, Func<string, T, Task<CatalogResult>> change, CancellationToken cancellationToken) where T : class
    {
        var (admin, refusal) = await admins.CheckAsync(auth, request, cancellationToken);
        if (admin is null)
        {
            return refusal!;
        }

        return await ApiResults.ReadAsync<T>(request, cancellationToken) is { } body
            ? await AnswerAsync(await change(admin, body), cancellationToken)
            : ApiResults.BadRequest(ApiErrors.BadBody);
    }

    private async Task<IActionResult> RunAsync(HttpRequest request, Func<string, Task<CatalogResult>> change, CancellationToken cancellationToken)
    {
        var (admin, refusal) = await admins.CheckAsync(auth, request, cancellationToken);
        return admin is null ? refusal! : await AnswerAsync(await change(admin), cancellationToken);
    }

    private async Task<IActionResult> AnswerAsync(CatalogResult result, CancellationToken cancellationToken) => result.Outcome switch
    {
        CatalogOutcome.Saved => new OkObjectResult(await store.ReadAsync(cancellationToken)),
        CatalogOutcome.NotFound => new NotFoundResult(),
        CatalogOutcome.OthersStays => ApiResults.Conflict(ApiErrors.OthersStays),
        CatalogOutcome.NeedsPrice => ApiResults.Conflict(ApiErrors.NeedsPrice),
        // The field travels in the code ("invalid-english"), so the site can name it even with error details off.
        _ => ApiResults.BadRequest(ApiErrors.Invalid(result.Field ?? "body")),
    };
}
