#if ANDROID
using Firebase;
using Firebase.Auth;
using Java.Lang;
using Java.Util.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;
using Exception = System.Exception;

namespace MobileAppTheme.Firebase;

/// <summary>
/// Sending an SMS code on Android through Firebase's own SDK instead of the plugin. Plugin.Firebase (3.1.x and 4.2.x,
/// fixed only after them) returns as soon as it asks Firebase to send, keeps only the session id from "code sent", and
/// throws away the other answers: a send Firebase refused never said so (the code box waited for an SMS that never came),
/// a number Android confirmed by itself (instant verification, no code to type) never signed in, and a "code sent" that
/// arrived after the app had gone to the background (the reCAPTCHA browser page, or reading the SMS: common on Xiaomi)
/// left no session id, so the right six digits were refused. Here each send waits for Firebase's answer, keeps every
/// one of them for that send only, and a late answer to an older send is ignored.
/// </summary>
internal sealed class AndroidPhoneVerification(ILogger logger)
{
    /// <summary>How long Android waits to read the SMS by itself before giving up (Firebase's own limit).</summary>
    private const long AutoReadSeconds = 60;

    private Attempt? _current;

    /// <summary>
    /// Asks Firebase to send a code to <paramref name="phoneNumber"/> (E.164) and waits for its answer: done once the
    /// code is sent or the number confirmed by itself; Firebase's exception when it refused.
    /// </summary>
    public Task SendAsync(string phoneNumber)
    {
        var activity = Platform.CurrentActivity
            ?? throw new InvalidOperationException("There is no screen to send the SMS code from.");

        // "Send a new code" to the same number: the resend token makes Firebase send a new SMS and answer again, instead
        // of quietly reusing its open session (which never says "code sent" a second time).
        var resend = _current is { } previous && previous.PhoneNumber == phoneNumber ? previous.ResendToken : null;
        var attempt = new Attempt(phoneNumber);
        _current = attempt;

        var options = PhoneAuthOptions.NewBuilder()
            .SetPhoneNumber(phoneNumber)
            .SetTimeout(Long.ValueOf(AutoReadSeconds), TimeUnit.Seconds!)
            .SetActivity(activity)
            .SetCallbacks(new Callbacks(this, attempt));
        if (resend is not null)
        {
            options.SetForceResendingToken(resend);
        }

        PhoneAuthProvider.VerifyPhoneNumber(options.Build());
        return attempt.Sent.Task;
    }

    /// <summary>
    /// The credential for the code the person typed: the SMS session with the code, or, when Firebase never said "code
    /// sent" but confirmed the number by itself, that confirmation. Throws when no code was asked for.
    /// </summary>
    public PhoneAuthCredential CredentialFor(string code)
    {
        var attempt = _current ?? throw new InvalidOperationException("No SMS code was sent in this run of the app.");
        if (attempt.VerificationId is { Length: > 0 } sessionId)
        {
            return PhoneAuthProvider.GetCredential(sessionId, code);
        }

        return attempt.Confirmed ?? throw new InvalidOperationException("Firebase never said the SMS code was sent.");
    }

    private void CodeSent(Attempt attempt, string verificationId, PhoneAuthProvider.ForceResendingToken? token)
    {
        attempt.VerificationId = verificationId;
        attempt.ResendToken = token ?? attempt.ResendToken;
        attempt.Sent.TrySetResult();
    }

    private void AutoReadTimedOut(Attempt attempt, string verificationId)
    {
        // Comes after "code sent"; if that answer was lost, this is where the session id still arrives.
        attempt.VerificationId ??= verificationId;
        attempt.Sent.TrySetResult();
    }

    private void Completed(Attempt attempt, PhoneAuthCredential credential)
    {
        attempt.Confirmed = credential;
        attempt.Sent.TrySetResult();

        // Android read the SMS (or confirmed the number with no SMS at all): sign in with it when nobody is signed in.
        // The code page sees the person signed in with that number and goes on (PhoneSignIn). Adding a number to a
        // signed-in account still waits for the typed code (LinkPhoneAsync).
        if (_current == attempt && FirebaseAuth.Instance.CurrentUser is null)
        {
            _ = SignInAsync(credential);
        }
    }

    private void Failed(Attempt attempt, FirebaseException exception)
    {
        if (!attempt.Sent.TrySetException(exception))
        {
            logger.LogWarning(exception, "Firebase reported a phone verification failure after the code was sent.");
        }
    }

    private async Task SignInAsync(PhoneAuthCredential credential)
    {
        try
        {
            await FirebaseAuth.Instance.SignInWithCredentialAsync(credential);
            logger.LogInformation("Android confirmed the number by itself; signed in.");
        }
        catch (Exception ex)
        {
            // The person can still type the code.
            logger.LogWarning(ex, "Signing in with the number Android confirmed by itself failed.");
        }
    }

    /// <summary>One send: its number and every answer Firebase gave to it.</summary>
    private sealed class Attempt(string phoneNumber)
    {
        public string PhoneNumber { get; } = phoneNumber;
        public TaskCompletionSource Sent { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public string? VerificationId { get; set; }
        public PhoneAuthProvider.ForceResendingToken? ResendToken { get; set; }
        public PhoneAuthCredential? Confirmed { get; set; }
    }

    /// <summary>Firebase's answers to one send, passed on only while that send is the latest.</summary>
    private sealed class Callbacks(AndroidPhoneVerification owner, Attempt attempt)
        : PhoneAuthProvider.OnVerificationStateChangedCallbacks
    {
        private bool IsLatest => owner._current == attempt;

        public override void OnCodeSent(string verificationId, PhoneAuthProvider.ForceResendingToken forceResendingToken)
        {
            base.OnCodeSent(verificationId, forceResendingToken);
            if (IsLatest)
            {
                owner.CodeSent(attempt, verificationId, forceResendingToken);
            }
        }

        public override void OnCodeAutoRetrievalTimeOut(string verificationId)
        {
            base.OnCodeAutoRetrievalTimeOut(verificationId);
            if (IsLatest)
            {
                owner.AutoReadTimedOut(attempt, verificationId);
            }
        }

        public override void OnVerificationCompleted(PhoneAuthCredential credential)
        {
            if (IsLatest)
            {
                owner.Completed(attempt, credential);
            }
        }

        public override void OnVerificationFailed(FirebaseException exception)
        {
            if (IsLatest)
            {
                owner.Failed(attempt, exception);
            }
        }
    }
}
#endif
