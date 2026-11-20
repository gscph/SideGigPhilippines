namespace MobileAppTheme.Auth;

/// <summary>
/// When the sign-in sheet's code step offers the other ways in (Google, Apple): an SMS can be late or never come
/// (carrier filtering, the provider refusing to send), so after <see cref="ShowAfter"/> without a code, or once
/// today's codes are used up, the person gets a way out instead of waiting on a code that may not come.
/// </summary>
public static class CodeFallback
{
    /// <summary>How long the code step waits before offering the other ways in.</summary>
    public static readonly TimeSpan ShowAfter = TimeSpan.FromSeconds(60);

    /// <summary>Whether to offer the other ways in, <paramref name="sinceSent"/> after the first code went out.</summary>
    /// <param name="codesUsedUp">No more codes today (<see cref="SmsSendLimit.WaitLeft"/> is null).</param>
    public static bool ShowOtherSignIn(TimeSpan sinceSent, bool codesUsedUp) => codesUsedUp || sinceSent >= ShowAfter;
}
