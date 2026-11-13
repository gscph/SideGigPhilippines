using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Shared;

/// <summary>
/// The Skilled Worker's trade as a brand-colored badge ("❄️ Aircon Technician", "🚰 Tubero"), on the pro card and
/// the public profile. Shows nothing for a trade we no longer offer.
/// </summary>
public partial class TradeBadge : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    [Parameter, EditorRequired] public ProfessionalListing Listing { get; set; } = default!;

    [Parameter] public MudBlazor.Size Size { get; set; } = MudBlazor.Size.Small;

    /// <summary>Centered under the profile photo; start-aligned on the card.</summary>
    [Parameter] public MudBlazor.Justify Justify { get; set; } = MudBlazor.Justify.FlexStart;

    [Parameter] public string? Class { get; set; }

    private IReadOnlyList<CatalogOption> Trades => Listing.TradeOptions();

    private string Text(CatalogOption trade) =>
        string.IsNullOrEmpty(trade.Icon) ? T.Get(trade.Label) : $"{trade.Icon} {T.Get(trade.Label)}";
}
