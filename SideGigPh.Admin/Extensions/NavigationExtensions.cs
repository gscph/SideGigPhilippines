using Microsoft.AspNetCore.Components;

namespace SideGigPh.Admin.Extensions;

public static class NavigationExtensions
{
    /// <summary>
    /// Writes a list's search and filter into the address (replacing the history entry, no reload), so a reload or a
    /// shared link opens the same view. Empty values are taken out.
    /// </summary>
    public static void KeepInAddress(this NavigationManager navigation, params (string Name, string? Value)[] values)
    {
        var parameters = values.ToDictionary(v => v.Name, v => (object?)(string.IsNullOrWhiteSpace(v.Value) ? null : v.Value.Trim()));
        navigation.NavigateTo(navigation.GetUriWithQueryParameters(parameters), replace: true);
    }
}
