using Microsoft.AspNetCore.Components;
using MobileAppTheme.Auth;
using MobileAppTheme.Localization;

namespace MobileAppTheme.Components.Auth;

/// <summary>
/// Phone sign-in in two steps: the number (read with the app's <see cref="IPhoneNumberFormat"/>),
/// then the SMS code. Reports the <see cref="AuthResult"/> to the parent, failures included.
/// With <see cref="LinkToCurrentUser"/> the same two steps add the number to the signed-in account.
/// Codes are rationed by <see cref="SmsSendLimit"/> (another code to the same number after 6 minutes, 3 a day): the
/// buttons count down the wait, and when today's codes are used up the person is told to try tomorrow (or, signing in,
/// to continue with Google).
/// </summary>
public partial class PhoneSignIn : ComponentBase, IDisposable
{
    private enum Step
    {
        EnterPhone,
        EnterCode,
    }

    [Inject] private IIdentityProvider Identity { get; set; } = default!;
    [Inject] private IPhoneNumberFormat PhoneFormat { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;
    [Inject] private SmsSendLimit Limit { get; set; } = default!;

    [Parameter] public EventCallback<AuthResult> OnAuthenticated { get; set; }

    /// <summary>Add the verified number to the signed-in account instead of signing in with it.</summary>
    [Parameter] public bool LinkToCurrentUser { get; set; }

    /// <summary>
    /// The number the code went to (E.164) once it's sent, null again when the user goes back to
    /// the number. The page uses it to switch its header to "Confirm your number".
    /// </summary>
    [Parameter] public EventCallback<string?> SentToChanged { get; set; }

    private Step _step = Step.EnterPhone;
    private string _phoneNumber = string.Empty;
    private string _code = string.Empty;
    private string? _verificationId;
    private string? _sentTo;
    private bool _busy;
    private Timer? _ticker;
    private bool _counting;

    // The result went to the parent (typed code, or the provider confirmed the number itself): never twice.
    private bool _finished;

    /// <summary>How many digits the SMS code has.</summary>
    private const int CodeLength = 6;

    // The last send timed out: an SMS may still arrive, but there is no code box to type it in.
    private bool _timedOut;

    // The number a code would go to now: the typed one on the number step, the one it went to on the code step.
    private string? Target => _step == Step.EnterCode
        ? _sentTo
        : PhoneFormat.TryNormalize(_phoneNumber, out var e164) ? e164 : null;

    // Zero: a code may be sent now; null: today's codes are used up; else how long to wait.
    private TimeSpan? WaitLeft => Limit.WaitLeft(Target);

    private string TooManyCodes => T.Format(
        OffersOtherSignIn ? CommonCopy.AuthTooManyCodes : CommonCopy.AuthTooManyCodesTomorrow, SmsSendLimit.PerDay);

    // "Or continue with Google" only where it is a way out: signing in (not adding a number) with Google or Apple offered.
    private bool OffersOtherSignIn =>
        !LinkToCurrentUser && (Identity.Methods.HasFlag(AuthMethods.Google) || Identity.Methods.HasFlag(AuthMethods.Apple));

    private bool CanSend => WaitLeft == TimeSpan.Zero;

    // The countdown, e.g. "5:12".
    private string WaitText => WaitLeft is { } left ? $"{(int)left.TotalMinutes}:{left.Seconds:00}" : string.Empty;

    // Re-draw once a second while a wait is counting down, and once more when it ends, so the buttons unlock on their own.
    protected override void OnInitialized() =>
        _ticker = new Timer(_ => _ = InvokeAsync(Tick), null, 1000, 1000);

    private async Task Tick()
    {
        var counting = WaitLeft is { } left && left > TimeSpan.Zero;
        if (counting || _counting)
        {
            StateHasChanged();
        }

        _counting = counting;
        await SignedInMeanwhileAsync();
    }

    /// <summary>
    /// Android may read the SMS and confirm the number by itself while the code step is open (the provider then signs
    /// the person in, and the code they type next is refused as used). Signing in, not adding a number: once the
    /// provider has someone signed in with the number the code went to, go on as if the code was typed.
    /// </summary>
    private async Task SignedInMeanwhileAsync()
    {
        if (_step != Step.EnterCode || LinkToCurrentUser || _busy || _finished || _sentTo is null)
        {
            return;
        }

        AuthUser? user;
        try
        {
            user = await Identity.GetCurrentUserAsync();
        }
        catch (Exception)
        {
            return; // runs off a timer: a failed look is simply tried again next second
        }

        if (user is not null && user.PhoneNumber == _sentTo && !_finished && !_busy)
        {
            _finished = true;
            try
            {
                await OnAuthenticated.InvokeAsync(AuthResult.Ok(user));
            }
            catch (Exception)
            {
                // Runs off a timer, where nothing would catch it: let the person carry on with the code instead.
                _finished = false;
            }
        }
    }

    /// <summary>The code box: the sixth digit sends it, so nobody has to find the button.</summary>
    private async Task CodeChangedAsync(string? value)
    {
        _code = value ?? string.Empty;
        if (_code.Trim().Length == CodeLength && _code.Trim().All(char.IsAsciiDigit))
        {
            await VerifyAsync();
        }
    }

    /// <summary>Back to the number step (the page's back arrow, or "Use a different number").</summary>
    public async Task UseOtherNumberAsync()
    {
        _step = Step.EnterPhone;
        _code = string.Empty;
        _verificationId = null;
        _sentTo = null;
        await SentToChanged.InvokeAsync(null);
        StateHasChanged();
    }

    // "Send a new code" on the code step: the same number again, once the wait is over.
    private Task ResendAsync()
    {
        _phoneNumber = _sentTo ?? _phoneNumber;
        return SendCodeAsync();
    }

    private async Task SendCodeAsync()
    {
        if (_busy)
        {
            return;
        }
        if (!PhoneFormat.TryNormalize(_phoneNumber, out var e164))
        {
            await OnAuthenticated.InvokeAsync(AuthResult.Fail(AuthError.InvalidPhoneNumber));
            return;
        }
        if (Limit.WaitLeft(e164) != TimeSpan.Zero)
        {
            return;
        }

        _busy = true;
        try
        {
            var verification = await Identity.SignUpWithPhoneAsync(e164);
            Limit.Record(e164);
            _timedOut = false;
            _verificationId = verification.VerificationId;
            _sentTo = e164;
            _step = Step.EnterCode;
            await SentToChanged.InvokeAsync(e164);
        }
        catch (TimeoutException ex)
        {
            // The provider never answered (no signal, or its app check stalled): say so instead of spinning. The SMS may
            // still have gone out, so it counts: an instant retry would pay for a second one.
            Limit.Record(e164);
            _timedOut = true;
            await OnAuthenticated.InvokeAsync(AuthResult.Fail(AuthError.Network, ex.Message));
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
        if (_busy)
        {
            return;
        }
        if (_verificationId is null || _finished)
        {
            return;
        }

        _busy = true;
        try
        {
            var code = _code.Trim();
            var result = LinkToCurrentUser
                ? await Identity.LinkPhoneAsync(_verificationId, code)
                : await Identity.VerifyPhoneOtpAsync(_verificationId, code);
            _finished = result.IsSuccess;
            await OnAuthenticated.InvokeAsync(result);
        }
        catch (Exception ex)
        {
            // Adapters answer failures as results; this is the backstop so the button always comes back.
            await OnAuthenticated.InvokeAsync(AuthResult.Fail(AuthError.Unknown, ex.Message));
        }
        finally
        {
            _busy = false;
        }
    }

    public void Dispose() => _ticker?.Dispose();
}
