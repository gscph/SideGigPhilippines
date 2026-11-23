using Microsoft.AspNetCore.Components;
using SideGigPh.FrontDoor.EarlyAccess;

namespace SideGigPh.FrontDoor.Components.Landing;

/// <summary>The slim last call: back up to the form, or, once this browser signed up, a thank-you and Share.</summary>
public partial class RepeatCta
{
    [Inject] private PageScript Script { get; set; } = default!;
    [Inject] private SignupState Signup { get; set; } = default!;

    private bool _copied;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        Signup.Changed += OnSignupChanged;
    }

    private Task ToFormAsync() => Script.ScrollToAsync(Sections.EarlyAccess, Sections.EmailFieldSelector);

    private async Task ShareAsync()
    {
        _copied = await Script.ShareAsync("Tarbaho", T(Localization.Copy.Join.ShareText), Sections.SiteUrl) == "copied";
    }

    private void OnSignupChanged() => InvokeAsync(StateHasChanged);

    public override void Dispose()
    {
        Signup.Changed -= OnSignupChanged;
        base.Dispose();
    }
}
