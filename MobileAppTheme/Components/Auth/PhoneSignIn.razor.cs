using Microsoft.AspNetCore.Components;
using MobileAppTheme.Auth;
using MobileAppTheme.Localization;

namespace MobileAppTheme.Components.Auth;

/// <summary>
/// Phone sign-in in two steps: the number (read with the app's <see cref="IPhoneNumberFormat"/>),
/// then the SMS code. Reports the <see cref="AuthResult"/> to the parent, failures included.
/// </summary>
public partial class PhoneSignIn : ComponentBase
{
    private enum Step
    {
        EnterPhone,
        EnterCode,
    }

    [Inject] private IIdentityProvider Identity { get; set; } = default!;
    [Inject] private IPhoneNumberFormat PhoneFormat { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;

    [Parameter] public EventCallback<AuthResult> OnAuthenticated { get; set; }

    private Step _step = Step.EnterPhone;
    private string _phoneNumber = string.Empty;
    private string _code = string.Empty;
    private string? _verificationId;
    private bool _busy;

    private async Task SendCodeAsync()
    {
        if (!PhoneFormat.TryNormalize(_phoneNumber, out var e164))
        {
            await OnAuthenticated.InvokeAsync(AuthResult.Fail(AuthError.InvalidPhoneNumber));
            return;
        }

        _busy = true;
        try
        {
            var verification = await Identity.SignUpWithPhoneAsync(e164);
            _verificationId = verification.VerificationId;
            _step = Step.EnterCode;
        }
        catch (Exception ex)
        {
            // The adapter has already logged the full exception.
            await OnAuthenticated.InvokeAsync(AuthResult.Fail(AuthError.Unknown, ex.Message));
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task VerifyAsync()
    {
        if (_verificationId is null)
        {
            return;
        }

        _busy = true;
        try
        {
            var result = await Identity.VerifyPhoneOtpAsync(_verificationId, _code.Trim());
            await OnAuthenticated.InvokeAsync(result);
        }
        finally
        {
            _busy = false;
        }
    }

    private void Reset()
    {
        _step = Step.EnterPhone;
        _code = string.Empty;
        _verificationId = null;
    }
}
