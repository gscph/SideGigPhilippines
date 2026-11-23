using SideGigPh.Contracts;

namespace SideGigPh.FrontDoor.EarlyAccess;

/// <summary>
/// What the sections share about the sign-up: the side the visitor picked in "How it works" (it pre-fills the form's
/// "I am a" unless they changed it there) and the address this browser already signed up with (remembered in
/// localStorage; the form opens on "you're on the list" and the repeat button turns into Share).
/// </summary>
public sealed class SignupState
{
    public string? SuggestedRole { get; private set; }

    public string? JoinedEmail { get; private set; }

    public bool HasJoined => JoinedEmail is not null;

    public event Action? Changed;

    public void SuggestRole(string role)
    {
        SuggestedRole = EarlyAccessRules.CleanRole(role);
        Changed?.Invoke();
    }

    public void SetJoined(string? email)
    {
        JoinedEmail = string.IsNullOrWhiteSpace(email) ? null : email;
        Changed?.Invoke();
    }
}
