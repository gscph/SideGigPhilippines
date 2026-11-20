using Microsoft.AspNetCore.Components;
using SideGigPh.Contracts;

namespace SideGigPh.Admin.Components.Dashboard.Views;

/// <summary>A "by trade / by town" breakdown: each label, its count and a bar against the biggest.</summary>
public partial class CountTable
{
    [Parameter, EditorRequired] public string Title { get; set; } = "";
    [Parameter, EditorRequired] public IReadOnlyList<AdminCountDto> Rows { get; set; } = [];

    private double Share(AdminCountDto row)
    {
        var top = Rows.Max(r => r.Count);
        return top == 0 ? 0 : row.Count * 100.0 / top;
    }
}
