using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using SideGigPh.AzureSQL;
using SideGigPh.Contracts;

namespace SideGigPh.Api;

/// <summary>
/// The operator site's reads (<c>SideGigPh.Admin</c>): the dashboard's numbers and the lists of accounts, households,
/// skilled workers and error reports, a page at a time. Only the uids in <c>AdminUids</c> (<see cref="AdminList"/>);
/// anyone else gets 401 or 403 <c>not-admin</c>. Read only: nothing here changes data. Contact details come masked.
/// </summary>
public sealed class AdminFunctions(FirebaseTokenValidator auth, AdminList admins, AdminStore store)
{
    /// <summary><c>GET admin/stats</c>: users, activity and what waits for the operator, with sign-ups per Philippine day.</summary>
    [Function("AdminStats")]
    public async Task<IActionResult> Stats(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "admin/stats")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        var (admin, refusal) = await admins.CheckAsync(auth, request, cancellationToken);
        return admin is null ? refusal! : new OkObjectResult(await store.StatsAsync(DateTimeOffset.UtcNow, cancellationToken));
    }

    /// <summary><c>GET admin/users?role=seeker|professional|none&amp;search=&amp;sort=joined|active|name&amp;desc=true&amp;skip=0&amp;take=25</c>.</summary>
    [Function("AdminUsers")]
    public async Task<IActionResult> Users(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "admin/users")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        var (admin, refusal) = await admins.CheckAsync(auth, request, cancellationToken);
        return admin is null ? refusal! : new OkObjectResult(await store.UsersAsync(ListQuery(request, "role"), cancellationToken));
    }

    /// <summary><c>GET admin/seekers?town=&amp;search=&amp;sort=joined|active|name|posts|completed&amp;desc=true&amp;skip=0&amp;take=25</c>.</summary>
    [Function("AdminSeekers")]
    public async Task<IActionResult> Seekers(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "admin/seekers")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        var (admin, refusal) = await admins.CheckAsync(auth, request, cancellationToken);
        return admin is null ? refusal! : new OkObjectResult(await store.SeekersAsync(ListQuery(request, "town"), cancellationToken));
    }

    /// <summary><c>GET admin/workers?filter=live|not-live|{tradeId}&amp;search=&amp;sort=joined|active|name|rating|completed|rate&amp;desc=true&amp;skip=0&amp;take=25</c>.</summary>
    [Function("AdminWorkers")]
    public async Task<IActionResult> Workers(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "admin/workers")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        var (admin, refusal) = await admins.CheckAsync(auth, request, cancellationToken);
        return admin is null ? refusal! : new OkObjectResult(await store.WorkersAsync(ListQuery(request, "filter"), cancellationToken));
    }

    /// <summary><c>GET admin/error-reports?filter=error|crash|…|dev|beta|prod&amp;search=&amp;sort=received|last|count&amp;desc=true&amp;skip=0&amp;take=25</c>.</summary>
    [Function("AdminErrorReports")]
    public async Task<IActionResult> ErrorReports(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "admin/error-reports")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        var (admin, refusal) = await admins.CheckAsync(auth, request, cancellationToken);
        return admin is null ? refusal! : new OkObjectResult(await store.ErrorReportsAsync(ListQuery(request, "filter"), cancellationToken));
    }

    /// <summary>The page the query string asks for; <paramref name="filterName"/> names the list's one filter.</summary>
    internal static AdminListQuery ListQuery(HttpRequest request, string filterName) => new(
        Search: request.Query["search"],
        Sort: request.Query["sort"],
        Desc: !string.Equals(request.Query["desc"], "false", StringComparison.OrdinalIgnoreCase),
        Skip: int.TryParse(request.Query["skip"], out var skip) ? skip : 0,
        Take: int.TryParse(request.Query["take"], out var take) ? take : AdminLists.DefaultTake,
        Filter: string.IsNullOrWhiteSpace(request.Query[filterName]) ? null : request.Query[filterName].ToString().Trim());
}
