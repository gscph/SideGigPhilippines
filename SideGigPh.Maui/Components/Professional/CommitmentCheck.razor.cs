using Microsoft.AspNetCore.Components;
using MudBlazor;
using SideGigPh.Maui.Services;

namespace SideGigPh.Maui.Components.Professional;

/// <summary>
/// One promise on the Publish checklist (<see cref="PublishChecklist"/>): its icon, a checkbox, and its words beside it in
/// one column that lines up with the box's top (a tap on the words ticks it too), with, for the Terms, a link to read them
/// under the words (<see cref="AppBrand.Legal"/>, as the sign-in consent line does).
/// </summary>
public partial class CommitmentCheck : ComponentBase
{
    [Inject] private AppBrand Brand { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;

    [Parameter, EditorRequired] public Commitment Item { get; set; } = default!;

    [Parameter] public bool Ticked { get; set; }

    [Parameter] public EventCallback<bool> TickedChanged { get; set; }

    private Task ToggleAsync() => TickedChanged.InvokeAsync(!Ticked);

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
