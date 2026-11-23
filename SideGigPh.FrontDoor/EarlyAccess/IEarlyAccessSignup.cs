using SideGigPh.Contracts;

namespace SideGigPh.FrontDoor.EarlyAccess;

public enum SignupResult { Sent, InvalidEmail, TooManyTries, Offline, Failed }

/// <summary>Port: sends an Early Access sign-up. Never throws; the form shows the result.</summary>
public interface IEarlyAccessSignup
{
    Task<SignupResult> JoinAsync(EarlyAccessRequest request, CancellationToken ct = default);
}
