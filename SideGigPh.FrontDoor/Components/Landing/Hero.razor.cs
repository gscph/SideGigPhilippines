using Microsoft.AspNetCore.Components;
using SideGigPh.Contracts;
using SideGigPh.FrontDoor.EarlyAccess;

namespace SideGigPh.FrontDoor.Components.Landing;

/// <summary>
/// The purple band at the top: logo, what Tarbaho is in five seconds, one button to the form, a link for workers, the
/// store tiles, and the app itself (<see cref="HeroPhones"/>). Text first on a phone, beside the screens on a wide one.
/// </summary>
public partial class Hero
{
    [Inject] private PageScript Script { get; set; } = default!;
    [Inject] private SignupState Signup { get; set; } = default!;

    private Task ToFormAsync() => Script.ScrollToAsync(Sections.EarlyAccess, Sections.EmailFieldSelector);

    private Task ToWorkersAsync()
    {
        Signup.SuggestRole(EarlyAccessRules.Worker);
        return Script.ScrollToAsync(Sections.HowItWorks);
    }
}
