using Microsoft.AspNetCore.Components;
using MobileAppTheme.Branding;
using MudBlazor;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Professional;

/// <summary>
/// One promise on the Publish checklist (<see cref="PublishChecklist"/>): its icon, a checkbox with its words, and for
/// the Terms a link to read them (<see cref="AppBrand.Legal"/>, as the sign-in consent line does).
/// </summary>
public partial class CommitmentCheck : ComponentBase
{
    [Inject] private AppBrand Brand { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;

    [Parameter, EditorRequired] public Commitment Item { get; set; } = default!;

    [Parameter] public bool Ticked { get; set; }

    [Parameter] public EventCallback<bool> TickedChanged { get; set; }

    // A picture beside each promise for readers who go by icons; the words stay the promise.
    private string Icon => Item.Id switch
    {
        PublishChecklist.Honest => Icons.Material.Rounded.Handshake,
        PublishChecklist.Terms => Icons.Material.Rounded.Gavel,
        PublishChecklist.Community => Icons.Material.Rounded.Groups,
        PublishChecklist.FairOffers => Icons.Material.Rounded.Sell,
        _ => Icons.Material.Rounded.CheckCircle,
    };
}
