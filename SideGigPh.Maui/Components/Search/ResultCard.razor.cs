using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Search;

/// <summary>One search result: the pro's card, with "Fits your budget" when it does. Tapping opens their profile.</summary>
public partial class ResultCard : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    [Parameter, EditorRequired] public SearchHit Hit { get; set; } = default!;
}
