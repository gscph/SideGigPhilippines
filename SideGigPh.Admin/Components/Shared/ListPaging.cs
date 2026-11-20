using MudBlazor;

namespace SideGigPh.Admin.Components.Shared;

/// <summary>What every list grid shares: its page sizes and the empty page shown when a load fails.</summary>
public static class ListPaging
{
    public static readonly int[] PageSizes = [25, 50, 100];

    public static GridData<T> Empty<T>() => new() { Items = [], TotalItems = 0 };
}
