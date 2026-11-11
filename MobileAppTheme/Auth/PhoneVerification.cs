namespace MobileAppTheme.Auth;

/// <summary>
/// Handle returned after an OTP is sent; carries the verification id used to
/// confirm the code the user types in the next step.
/// </summary>
public sealed class PhoneVerification
{
    public required string VerificationId { get; init; }
}
