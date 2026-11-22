namespace MobileAppTheme.Firebase;

/// <summary>What adding an SMS-verified number to the signed-in account did (<see cref="FirebaseIdentityProvider.LinkPhoneAsync"/>).</summary>
internal enum PhoneLinkOutcome
{
    /// <summary>The number is now on the signed-in account (same uid).</summary>
    Linked,

    /// <summary>The number was on another account, which is now signed in instead, with the same code.</summary>
    SwitchedAccount,
}
