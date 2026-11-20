#if ANDROID
using Android.App;
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
/// one of them for that send only, and a late answer to an older send is ignored. Firebase drops a send's callbacks
/// when the screen stops (its activity scope), so a send still waiting when the app comes back asks again for the same
/// number (<see cref="ScreenBack"/>, Firebase's own advice): no second SMS, and the answer reaches the new callbacks.
/// </summary>
internal sealed class AndroidPhoneVerification
{
    /// <summary>How long Android waits to read the SMS by itself before giving up (Firebase's own limit).</summary>
    private const long AutoReadSeconds = 60;

    // The provider is a singleton, so there is one of these; the activity's OnStart reaches it through this.
    private static AndroidPhoneVerification? s_instance;

    private readonly ILogger _logger;
    private Attempt? _current;

    public AndroidPhoneVerification(ILogger logger)
    {
        _logger = logger;
        s_instance = this;
    }

    /// <summary>
    /// The app's screen is showing again (activity OnStart, from <see cref="FirebaseAuthSetup"/>). A send still waiting
    /// for Firebase's answer lost its callbacks when the screen stopped (the person went to read the SMS, or the
    /// reCAPTCHA page opened): ask again for the same number so the answer has somewhere to go.
    /// </summary>
    public static void ScreenBack(Activity activity)
    {
        if (s_instance is { _current: { } attempt } owner && !attempt.Sent.Task.IsCompleted
            && DateTimeOffset.UtcNow - attempt.StartedAt < TimeSpan.FromSeconds(AutoReadSeconds))
        {
            owner._logger.LogInformation("The SMS code send was still waiting when the app came back; asking again.");
            owner.Start(activity, attempt, resend: null);
        }
    }

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

        Start(activity, attempt, resend);
        return attempt.Sent.Task;
    }

    private void Start(Activity activity, Attempt attempt, PhoneAuthProvider.ForceResendingToken? resend)
    {
        var options = PhoneAuthOptions.NewBuilder()
            .SetPhoneNumber(attempt.PhoneNumber)
            .SetTimeout(Long.ValueOf(AutoReadSeconds), TimeUnit.Seconds!)
            .SetActivity(activity)
            .SetCallbacks(new Callbacks(this, attempt));
        if (resend is not null)
        {
            options.SetForceResendingToken(resend);
        }

        PhoneAuthProvider.VerifyPhoneNumber(options.Build());
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
            _logger.LogWarning(exception, "Firebase reported a phone verification failure after the code was sent.");
        }
    }

    private async Task SignInAsync(PhoneAuthCredential credential)
    {
        try
        {
            await FirebaseAuth.Instance.SignInWithCredentialAsync(credential);
            _logger.LogInformation("Android confirmed the number by itself; signed in.");
        }
        catch (Exception ex)
        {
            // The person can still type the code.
            _logger.LogWarning(ex, "Signing in with the number Android confirmed by itself failed.");
        }
    }

    /// <summary>One send: its number and every answer Firebase gave to it.</summary>
    private sealed class Attempt(string phoneNumber)
    {
        public string PhoneNumber { get; } = phoneNumber;
        public DateTimeOffset StartedAt { get; } = DateTimeOffset.UtcNow;
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
