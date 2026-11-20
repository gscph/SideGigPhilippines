using SideGigPh.Admin.Services;
using SideGigPh.Contracts;

namespace SideGigPh.Admin.Components.Dashboard;

/// <summary>The dashboard's numbers (<c>GET admin/stats</c>): who signed up, what they did, what waits for the operator.</summary>
public sealed class DashboardService(AdminApi api)
{
    public Task<AdminStatsDto> GetStatsAsync(CancellationToken cancellationToken = default) =>
        api.GetAsync("admin/stats", ApiJson.Default.AdminStatsDto, cancellationToken);
}
