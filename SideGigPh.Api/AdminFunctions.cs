using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using SideGigPh.Api.Insights;
using SideGigPh.AzureSQL;
using SideGigPh.Contracts;

namespace SideGigPh.Api;

/// <summary>
/// The operator site's reads (<c>SideGigPh.Admin</c>): the dashboard's numbers and the lists of accounts, households,
/// skilled workers, error reports and "Others" notes, a page at a time. Only the uids in <c>AdminUids</c> (<see cref="AdminList"/>);
/// anyone else gets 401 or 403 <c>not-admin</c>. Read only: nothing here changes data. Contact details come masked.
/// </summary>
public sealed class AdminFunctions(FirebaseTokenValidator auth, AdminList admins, AdminStore store, OtherJobInsights otherJobs)
{
    /// <summary><c>GET ops/stats</c>: users, activity and what waits for the operator, with sign-ups per Philippine day.</summary>
    [Function("AdminStats")]
    public async Task<IActionResult> Stats(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "ops/stats")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        var (admin, refusal) = await admins.CheckAsync(auth, request, cancellationToken);
        return admin is null ? refusal! : new OkObjectResult(await store.StatsAsync(DateTimeOffset.UtcNow, cancellationToken));
    }

    /// <summary><c>GET ops/users?role=seeker|professional|none&amp;search=&amp;sort=joined|active|name&amp;desc=true&amp;skip=0&amp;take=25</c>.</summary>
    [Function("AdminUsers")]
    public async Task<IActionResult> Users(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "ops/users")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        var (admin, refusal) = await admins.CheckAsync(auth, request, cancellationToken);
        return admin is null ? refusal! : new OkObjectResult(await store.UsersAsync(ListQuery(request, "role"), cancellationToken));
    }

    /// <summary><c>GET ops/seekers?town=&amp;search=&amp;sort=joined|active|name|posts|completed&amp;desc=true&amp;skip=0&amp;take=25</c>.</summary>
    [Function("AdminSeekers")]
    public async Task<IActionResult> Seekers(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "ops/seekers")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        var (admin, refusal) = await admins.CheckAsync(auth, request, cancellationToken);
        return admin is null ? refusal! : new OkObjectResult(await store.SeekersAsync(ListQuery(request, "town"), cancellationToken));
    }

    /// <summary><c>GET ops/workers?filter=live|not-live|{tradeId}&amp;search=&amp;sort=joined|active|name|rating|completed|rate&amp;desc=true&amp;skip=0&amp;take=25</c>.</summary>
    [Function("AdminWorkers")]
    public async Task<IActionResult> Workers(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "ops/workers")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        var (admin, refusal) = await admins.CheckAsync(auth, request, cancellationToken);
        return admin is null ? refusal! : new OkObjectResult(await store.WorkersAsync(ListQuery(request, "filter"), cancellationToken));
    }

    /// <summary><c>GET ops/error-reports?filter=error|crash|…|dev|beta|prod&amp;search=&amp;sort=received|last|count&amp;desc=true&amp;skip=0&amp;take=25</c>.</summary>
    [Function("AdminErrorReports")]
    public async Task<IActionResult> ErrorReports(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "ops/error-reports")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        var (admin, refusal) = await admins.CheckAsync(auth, request, cancellationToken);
        return admin is null ? refusal! : new OkObjectResult(await store.ErrorReportsAsync(ListQuery(request, "filter"), cancellationToken));
    }

    /// <summary><c>GET ops/other-jobs?filter=seeker|professional|{tradeId}&amp;search=&amp;desc=true&amp;skip=0&amp;take=25</c>: what people typed under "Others", newest first.</summary>
    [Function("AdminOtherJobs")]
    public async Task<IActionResult> OtherJobs(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "ops/other-jobs")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        var (admin, refusal) = await admins.CheckAsync(auth, request, cancellationToken);
        return admin is null ? refusal! : new OkObjectResult(await store.OtherJobsAsync(ListQuery(request, "filter"), cancellationToken));
    }

    /// <summary>
    /// <c>GET ops/other-jobs/groups?filter=seeker|professional|{tradeId}&amp;days=</c>: what people typed under "Others",
    /// grouped (same text after cleanup, and same meaning when the embeddings model is set up), biggest first, from the
    /// newest <see cref="AdminStore.MaxNotesToGroup"/> notes of the last <c>days</c> days (all when missing).
    /// </summary>
    [Function("AdminOtherJobGroups")]
    public async Task<IActionResult> OtherJobGroups(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "ops/other-jobs/groups")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        var (admin, refusal) = await admins.CheckAsync(auth, request, cancellationToken);
        if (admin is null)
        {
            return refusal!;
        }

        var filter = string.IsNullOrWhiteSpace(request.Query["filter"]) ? null : request.Query["filter"].ToString().Trim();
        int? days = int.TryParse(request.Query["days"], out var d) && d > 0 ? d : null;
        return new OkObjectResult(await otherJobs.GroupsAsync(filter, days, cancellationToken));
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
