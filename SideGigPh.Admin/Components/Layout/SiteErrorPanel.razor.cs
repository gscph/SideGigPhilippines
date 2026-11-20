using Microsoft.AspNetCore.Components;
using SideGigPh.Admin.Components.ErrorReport;

namespace SideGigPh.Admin.Components.Layout;

/// <summary>
/// What the layout's error boundary shows when a page throws: the error, Try again, and the report sent once to
/// <c>POST error-reports</c> (<see cref="ErrorReportService.ReportSiteErrorAsync"/>), so it shows on the Error reports page.
/// </summary>
public partial class SiteErrorPanel
{
    [Inject] public required ErrorReportService Reports { get; set; }
    [Inject] public required NavigationManager Navigation { get; set; }

    [Parameter, EditorRequired] public Exception Exception { get; set; } = default!;
    [Parameter] public EventCallback OnRecover { get; set; }

    private Exception? _reported;

    protected override async Task OnParametersSetAsync()
    {
        if (ReferenceEquals(_reported, Exception))
        {
            return;
        }

        _reported = Exception;
        await Reports.ReportSiteErrorAsync(Exception, "/" + Navigation.ToBaseRelativePath(Navigation.Uri));
    }
}
