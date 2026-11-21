using MobileAppTheme.Services;

namespace SideGigPh.Maui.Services;

/// <summary>What a Call or Text tap did.</summary>
public enum ContactOutcome
{
    /// <summary>The phone's dialer or SMS app opened.</summary>
    Opened,

    /// <summary>No picked offer links the two any more (cancelled, blocked) or the other person has no number.</summary>
    NotLinked,

    /// <summary>The phone's app couldn't open.</summary>
    Failed,
}

/// <summary>
/// Call and Text on the chat pages once a picked offer links a household and a worker, without the app ever showing
/// the other person's number: the pages only learn whether they may call (<see cref="IsOpenAsync"/>), and each tap
/// reads the number from the server right then and hands it straight to the phone's own app
/// (<see cref="IContactLauncher"/>), so a block or a cancelled pick since the page opened is honoured. The dialer or
/// SMS app still shows the number: this makes taking the deal offline harder, not impossible.
/// </summary>
public sealed class PickedContact(IJobPostRepository posts, IContactLauncher phone)
{
    /// <summary>Whether Call and Text show: a picked offer links the two and the other person has a number.</summary>
    public async Task<bool> IsOpenAsync(string otherUid) => await posts.GetContactNumberAsync(otherUid) is not null;

    /// <summary>Opens the dialer on the other person's number, read from the server now.</summary>
    public Task<ContactOutcome> CallAsync(string otherUid) => LaunchAsync(otherUid, phone.CallAsync);

    /// <summary>Opens the SMS app to the other person with <paramref name="message"/> written in.</summary>
    public Task<ContactOutcome> TextAsync(string otherUid, string message) =>
        LaunchAsync(otherUid, number => phone.TextAsync(number, message));

    private async Task<ContactOutcome> LaunchAsync(string otherUid, Func<string, Task<bool>> open)
    {
        if (await posts.GetContactNumberAsync(otherUid) is not { } number)
        {
            return ContactOutcome.NotLinked;
        }

        return await open(number) ? ContactOutcome.Opened : ContactOutcome.Failed;
    }
}
