using Microsoft.AspNetCore.Components;
using SideGigPh.Admin.Services;

namespace SideGigPh.Admin.Components.Layout;

/// <summary>The signed-in operator's email in the top bar, with Sign out.</summary>
public partial class ProfileDisplay
{
    [Inject] public required AuthService Auth { get; set; }

    private bool _busy;

    private async Task SignOutAsync()
    {
        if (_busy) return;
        _busy = true;
        try
        {
            await Auth.SignOutAsync();
        }
        finally
        {
            _busy = false;
        }
    }
}
