using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;

namespace SideGigPh.Maui.Components.Auth;

/// <summary>Reusable phone OTP form. Emits the <see cref="AuthResult"/> to the parent.</summary>
public partial class PhoneSignIn : ComponentBase
{
    private enum Step
    {
        EnterPhone,
        EnterCode,
    }

    [Inject] private IIdentityProvider Identity { get; set; } = default!;

    [Parameter] public EventCallback<AuthResult> OnAuthenticated { get; set; }

    private Step _step = Step.EnterPhone;
    private string _phoneNumber = string.Empty;
    private string _code = string.Empty;
    private string? _verificationId;
    private bool _busy;

    private async Task SendCodeAsync()
    {
        _busy = true;
        try
        {
            var verification = await Identity.SignUpWithPhoneAsync(_phoneNumber.Trim());
            _verificationId = verification.VerificationId;
            _step = Step.EnterCode;
        }
        catch (Exception ex)
        {
            await OnAuthenticated.InvokeAsync(AuthResult.Fail(ex.Message));
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
