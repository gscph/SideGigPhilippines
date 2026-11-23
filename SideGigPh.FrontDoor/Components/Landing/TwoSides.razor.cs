using Microsoft.AspNetCore.Components;
using SideGigPh.Contracts;
using SideGigPh.FrontDoor.EarlyAccess;
using SideGigPh.FrontDoor.Localization;

namespace SideGigPh.FrontDoor.Components.Landing;

/// <summary>
/// "How it works", for a household or a Kuya / Ate: three steps each. The pick also fills the form's "I am a" (unless the
/// visitor changed it there), and the hero's worker link opens it on the worker side.
/// </summary>
public partial class TwoSides
{
    private sealed record Step(string Icon, Say Title, Say Text);

    private static readonly Step[] HouseholdSteps =
    [
        new("images/icons/search.svg", Copy.Sides.H1Title, Copy.Sides.H1Text),
        new("images/icons/chat.svg", Copy.Sides.H2Title, Copy.Sides.H2Text),
        new("images/icons/money.svg", Copy.Sides.H3Title, Copy.Sides.H3Text),
    ];

    private static readonly Step[] WorkerSteps =
    [
        new("images/icons/profile-card.svg", Copy.Sides.W1Title, Copy.Sides.W1Text),
        new("images/icons/megaphone.svg", Copy.Sides.W2Title, Copy.Sides.W2Text),
        new("images/icons/wallet.svg", Copy.Sides.W3Title, Copy.Sides.W3Text),
    ];

    [Inject] private SignupState Signup { get; set; } = default!;

    private string Side => Signup.SuggestedRole ?? EarlyAccessRules.Household;

    private bool IsWorker => Side == EarlyAccessRules.Worker;

    private Step[] Steps => IsWorker ? WorkerSteps : HouseholdSteps;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        Signup.Changed += OnSignupChanged;
    }

    private void PickSide(string side) => Signup.SuggestRole(side);

    private void OnSignupChanged() => InvokeAsync(StateHasChanged);

    public override void Dispose()
    {
        Signup.Changed -= OnSignupChanged;
        base.Dispose();
    }
}
