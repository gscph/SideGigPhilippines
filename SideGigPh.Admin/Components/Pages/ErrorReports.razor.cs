using Microsoft.AspNetCore.Components;

namespace SideGigPh.Admin.Components.Pages;

/// <summary>The apps' error reports (<see cref="ErrorReport.Views.ErrorReportListView"/>), opened with <c>?search=&amp;filter=</c> when given.</summary>
public partial class ErrorReports
{
    [SupplyParameterFromQuery] public string? Search { get; set; }
    [SupplyParameterFromQuery] public string? Filter { get; set; }
}
