using Microsoft.AspNetCore.Components;
using SideGigPh.Contracts;

namespace SideGigPh.Admin.Components.ErrorReport.Views;

/// <summary>An opened error report row: when, the whole message, the stack trace and the trail of pages before it.</summary>
public partial class ErrorReportDetail
{
    [Parameter, EditorRequired] public AdminErrorReportDto Report { get; set; } = default!;
}
