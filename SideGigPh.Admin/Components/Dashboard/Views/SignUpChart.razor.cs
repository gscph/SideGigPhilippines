using Microsoft.AspNetCore.Components;
using MudBlazor;
using SideGigPh.Contracts;

namespace SideGigPh.Admin.Components.Dashboard.Views;

/// <summary>New accounts per Philippine day, stacked by side (households, skilled workers, no side yet). Every 5th day is labelled.</summary>
public partial class SignUpChart
{
    [Parameter, EditorRequired] public IReadOnlyList<AdminDayDto> Days { get; set; } = [];

    private List<ChartSeries<double>> _series = [];
    private string[] _labels = [];

    protected override void OnParametersSet()
    {
        _labels = Days.Select((d, i) => i % 5 == 0 || i == Days.Count - 1 ? d.Day.ToString("d MMM") : "").ToArray();
        _series =
        [
            Series("Households", Days.Select(d => d.Seekers)),
            Series("Skilled workers", Days.Select(d => d.Workers)),
            Series("No side yet", Days.Select(d => d.NoRole)),
        ];
    }

    private static ChartSeries<double> Series(string name, IEnumerable<int> counts) =>
        new() { Name = name, Data = new ChartData<double>(counts.Select(c => (double)c).ToList()) };
}
