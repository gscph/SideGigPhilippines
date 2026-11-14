using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Services.Diagnostics;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Shared;

/// <summary>
/// The Me page's "Send error reports" switch (both sides), with what a report holds and never holds. On by default;
/// turning it off stops recording and deletes the reports waiting on the phone (<see cref="ErrorLog"/>).
/// </summary>
public partial class ErrorReportsSwitch : ComponentBase
{
    [Inject] private ErrorLog Log { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;

    private bool _on = true;

    protected override async Task OnInitializedAsync()
    {
        await Log.LoadAsync();
        _on = Log.SendReports;
    }

    private async Task SetAsync(bool on)
    {
        _on = on;
        await Log.SetSendReportsAsync(on);
    }
}
