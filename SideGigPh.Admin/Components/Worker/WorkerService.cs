using MudBlazor;
using SideGigPh.Admin.Services;
using SideGigPh.Contracts;

namespace SideGigPh.Admin.Components.Worker;

/// <summary>The skilled workers (<c>GET ops/workers</c>): every live listing and the worker accounts not live yet.</summary>
public sealed class WorkerService(AdminApi api)
{
    /// <summary>The API's <c>filter</c> for live listings only.</summary>
    public const string Live = "live";

    /// <summary>The API's <c>filter</c> for worker accounts with no listing yet.</summary>
    public const string NotLive = "not-live";

    /// <summary>The grid's column → the API's sort word.</summary>
    public static readonly IReadOnlyDictionary<string, string> Sorts = new Dictionary<string, string>
    {
        [nameof(AdminWorkerDto.CreatedAt)] = AdminLists.SortJoined,
        [nameof(AdminWorkerDto.LastActiveAt)] = AdminLists.SortActive,
        [nameof(AdminWorkerDto.Name)] = AdminLists.SortName,
        [nameof(AdminWorkerDto.Rating)] = AdminLists.SortRating,
        [nameof(AdminWorkerDto.CompletedJobs)] = AdminLists.SortCompleted,
        [nameof(AdminWorkerDto.RatePesos)] = AdminLists.SortRate,
    };

    /// <summary>A page; <paramref name="filter"/> is <see cref="Live"/>, <see cref="NotLive"/>, a trade id or empty for all.</summary>
    public async Task<GridData<AdminWorkerDto>> GetWorkersAsync(GridState<AdminWorkerDto> state, string? search, string? filter, CancellationToken cancellationToken)
    {
        var page = await api.GetAsync($"ops/workers?{state.ToAdminQuery(search, Sorts, ("filter", filter))}", ApiJson.Default.AdminWorkerPageDto, cancellationToken);
        return page.Items.ToGridData(page.Total);
    }

    /// <summary>What the worker does: the trade, or for "Others" what they typed.</summary>
    public static string Work(AdminWorkerDto worker) =>
        worker.OtherWork is { Length: > 0 } other ? $"{worker.Trade ?? "Others"}: {other}" : worker.Trade ?? "—";
}
