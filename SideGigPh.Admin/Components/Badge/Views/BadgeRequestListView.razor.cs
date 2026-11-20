using Microsoft.AspNetCore.Components;
using SideGigPh.Admin.Services;

namespace SideGigPh.Admin.Components.Badge.Views;

/// <summary>The waiting badge requests (<see cref="BadgeService"/>), one <see cref="BadgeRequestCard"/> each, oldest first.</summary>
public partial class BadgeRequestListView
{
    [Inject] public required BadgeService Badges { get; set; }

    private bool _loading = true;
    private string? _error;

    protected override Task OnInitializedAsync() => LoadAsync();

    private async Task LoadAsync()
    {
        _loading = true;
        _error = null;
        try
        {
            await Badges.LoadAsync();
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
