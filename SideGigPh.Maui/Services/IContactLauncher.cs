namespace SideGigPh.Maui.Services;

/// <summary>Hands a call or text off to the phone's own apps. Returns false if that isn't possible.</summary>
public interface IContactLauncher
{
    Task<bool> CallAsync(string phoneNumber);

    Task<bool> TextAsync(string phoneNumber, string message);
}
