using MudBlazor;
using SideGigPh.Admin.Components.Shared;
using SideGigPh.Admin.Services;
using SideGigPh.Contracts;

namespace SideGigPh.Admin.Components.ErrorReport;

/// <summary>
/// The problems the apps sent (<c>GET admin/error-reports</c>, kept 90 days, scrubbed of numbers, uids and emails on the
/// phone and again on the server), and this site's own crashes, sent to the same place (<see cref="ReportSiteErrorAsync"/>).
/// </summary>
public sealed class ErrorReportService(AdminApi api)
{
    /// <summary>How this site's own crashes are stamped, so they can be told from the phones' (search "operator").</summary>
    public const string SiteVersion = "operator";

    /// <summary>The grid's column → the API's sort word.</summary>
    public static readonly IReadOnlyDictionary<string, string> Sorts = new Dictionary<string, string>
    {
        [nameof(AdminErrorReportDto.LastAt)] = AdminLists.SortLast,
        [nameof(AdminErrorReportDto.Count)] = AdminLists.SortCount,
    };

    /// <summary>The filter's choices: a kind or a channel; empty = all.</summary>
    public static readonly IReadOnlyList<FilterChoice> Filters =
    [
        new("", "All"),
        new(ErrorKinds.Crash, "Crashes"),
        new(ErrorKinds.Error, "Errors"),
        new(ErrorKinds.Slow, "Stuck loading"),
        new(AppChannels.Prod, "Store builds (prod)"),
        new(AppChannels.Beta, "Beta builds"),
        new(AppChannels.Dev, "Dev builds"),
    ];

    private readonly string _installId = Guid.NewGuid().ToString();

    public async Task<GridData<AdminErrorReportDto>> GetReportsAsync(GridState<AdminErrorReportDto> state, string? search, string? filter, CancellationToken cancellationToken)
    {
        var page = await api.GetAsync($"admin/error-reports?{state.ToAdminQuery(search, Sorts, ("filter", filter))}", ApiJson.Default.AdminErrorReportPageDto, cancellationToken);
        return page.Items.ToGridData(page.Total);
    }

    /// <summary>
    /// Sends a crash of this site (an exception the layout's error boundary caught) to <c>POST error-reports</c>, like the
    /// phones do, so it shows on the Error reports page. Never throws: a report that doesn't go is only lost.
    /// </summary>
    public async Task ReportSiteErrorAsync(Exception exception, string? route)
    {
        var now = DateTimeOffset.UtcNow;
        var batch = new ErrorReportBatch(_installId, SiteVersion, SiteVersion, AppChannels.Prod, "web", null,
            [new ErrorReportDto(ErrorKinds.Crash, exception.GetType().FullName ?? "Exception", exception.Message, exception.ToString(), route, null, 1, now, now)]);
        try
        {
            await api.PostAsync("error-reports", batch, ApiJson.Default.ErrorReportBatch);
        }
        catch (AdminApiException)
        {
            // Lost; the page still shows the error.
        }
    }

    /// <summary>The color of a kind's chip: crashes red, stuck screens amber, the rest gray.</summary>
    public static Color KindColor(string kind) => kind switch
    {
        ErrorKinds.Crash => Color.Error,
        ErrorKinds.Slow => Color.Info,
        _ => Color.Default,
    };
}
