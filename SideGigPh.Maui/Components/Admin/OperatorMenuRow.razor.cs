using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Services;

namespace SideGigPh.Maui.Components.Admin;

/// <summary>The Me page's row to the badge review, shown only to an operator (<see cref="BadgeReview.IsOperatorAsync"/>).</summary>
public partial class OperatorMenuRow : ComponentBase
{
    [Inject] private BadgeReview Review { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;

    private bool _isOperator;

    protected override async Task OnInitializedAsync() => _isOperator = await Review.IsOperatorAsync();
}
