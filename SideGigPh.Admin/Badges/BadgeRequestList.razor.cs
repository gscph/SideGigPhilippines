using Microsoft.AspNetCore.Components;
using SideGigPh.Admin.Services;

namespace SideGigPh.Admin.Badges;

/// <summary>The waiting badge requests (<see cref="BadgeQueue"/>), one <see cref="BadgeRequestCard"/> each.</summary>
public partial class BadgeRequestList
{
    [Inject] private BadgeQueue Queue { get; set; } = default!;

    private bool _loading = true;
    private string? _error;

    protected override Task OnInitializedAsync() => LoadAsync();

    private async Task LoadAsync()
    {
        _loading = true;
        _error = null;
        try
        {
            await Queue.LoadAsync();
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
