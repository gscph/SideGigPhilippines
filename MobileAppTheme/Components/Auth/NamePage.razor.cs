using Microsoft.AspNetCore.Components;
using MobileAppTheme.Auth;
using MobileAppTheme.Localization;
using MobileAppTheme.Services;

namespace MobileAppTheme.Components.Auth;

/// <summary>
/// <c>/verify/name</c>: first and last name, asked once when the account has none (a phone
/// sign-in). Google sign-ins already have a name and never see this. Then goes to <c>then</c>, or
/// where the app's <see cref="IAuthLanding"/> says. With <c>edit=true</c> (<see cref="AuthRoutes.EditName"/>) it shows the
/// current name to change and has a Back: a name typed wrong at sign-up was otherwise there for good.
/// </summary>
public partial class NamePage : ComponentBase
{
    [Inject] private IIdentityProvider Identity { get; set; } = default!;
    [Inject] private IAuthLanding Landing { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;
    [Inject] private ITextCheck TextCheck { get; set; } = default!;

    [SupplyParameterFromQuery(Name = "then")] public string? Then { get; set; }

    [SupplyParameterFromQuery(Name = "edit")] public bool Edit { get; set; }

    private string? _firstName;
    private string? _lastName;
    private string? _error;
    private bool _busy;

    private bool CanSave =>
        !_busy && !string.IsNullOrWhiteSpace(_firstName) && !string.IsNullOrWhiteSpace(_lastName) && !FirstRude && !LastRude;

    private bool FirstRude => TextCheck.IsRude(_firstName);

    private bool LastRude => TextCheck.IsRude(_lastName);

    protected override async Task OnInitializedAsync()
    {
        var user = await Identity.GetCurrentUserAsync();
        if (user is null)
        {
            Nav.NavigateTo(AuthRoutes.SignIn, replace: true);
        }
        else if (Edit && user.DisplayName is { } current)
        {
            // "Juan Dela Cruz": the first word, then the rest (the page asks first and last name).
            var parts = current.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            _firstName = parts.ElementAtOrDefault(0);
            _lastName = parts.ElementAtOrDefault(1);
        }
        else if (user.HasName)
        {
            await GoOnAsync(user);
        }
    }

    // Only editing has a way back (first sign-up must give a name).
    private EventCallback BackHandler => Edit ? EventCallback.Factory.Create(this, GoBack) : default;

    private void GoBack() => Nav.NavigateTo(AuthRoutes.SafeReturn(Then) ?? "/", replace: true);

    private async Task SaveAsync()
    {
        if (!CanSave)
        {
            return; // a second tap before the button re-rendered as disabled
        }

        _busy = true;
        try
        {
            var result = await Identity.SetDisplayNameAsync($"{_firstName!.Trim()} {_lastName!.Trim()}");
            if (result is { IsSuccess: true, User: { } user })
            {
                await GoOnAsync(user);
                return;
            }

            _error = T.Describe(result.Error);
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task GoOnAsync(AuthUser user) =>
        Nav.NavigateTo(AuthRoutes.SafeReturn(Then) ?? await Landing.GetDestinationAsync(user), replace: true);
}
