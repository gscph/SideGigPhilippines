using Microsoft.AspNetCore.Components;
using SideGigPh.Admin.Extensions;
using SideGigPh.Admin.Services;
using SideGigPh.Contracts;

namespace SideGigPh.Admin.Components.Dashboard.Views;

/// <summary>
/// The first page: the app's numbers for a first look at how it's going (people, what they did, what needs the
/// operator), sign-ups per day and the breakdowns by trade, town and language. One call (<c>GET ops/stats</c>).
/// </summary>
public partial class DashboardView
{
    [Inject] public required DashboardService Dashboard { get; set; }

    private AdminStatsDto? _stats;
    private bool _loading;
    private string? _error;

    private string SubTitle => _stats is null ? "Loading the numbers…" : $"As of {_stats.At.ToManila()} (Philippine time)";

    protected override Task OnInitializedAsync() => LoadAsync();

    private async Task LoadAsync()
    {
        if (_loading) return;
        _loading = true;
        _error = null;
        try
        {
            _stats = await Dashboard.GetStatsAsync();
        }
        catch (AdminApiException ex)
        {
            _error = ex.Message;
        }
        finally
        {
            _loading = false;
        }
    }
}
