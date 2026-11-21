using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Shared;

/// <summary>
/// Call and Text in a chat's header, shown by both chat pages once a picked offer links the two. The number never
/// reaches the page: each tap goes through <see cref="PickedContact"/>, which reads it then and opens the phone's
/// dialer or SMS app. A problem is passed up as a message for the page to show.
/// </summary>
public partial class ContactButtons : ComponentBase
{
    [Inject] private PickedContact Contact { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;
    [Inject] private ILogger<ContactButtons> Logger { get; set; } = default!;

    /// <summary>The other person in the chat.</summary>
    [Parameter, EditorRequired] public string OtherUid { get; set; } = string.Empty;

    /// <summary>Their name, for the text's first line.</summary>
    [Parameter, EditorRequired] public string OtherName { get; set; } = string.Empty;

    /// <summary>The dialer opened (the household's page logs the call in the chat).</summary>
    [Parameter] public EventCallback OnCalled { get; set; }

    /// <summary>A message to show: null when a tap starts, else why nothing opened.</summary>
    [Parameter] public EventCallback<string?> OnProblem { get; set; }

    private bool _busy;

    private Task CallAsync() => RunAsync(() => Contact.CallAsync(OtherUid), OnCalled);

    private Task TextAsync() => RunAsync(() => Contact.TextAsync(OtherUid, T.Format(Copy.TextHello, OtherName)), default);

    private async Task RunAsync(Func<Task<ContactOutcome>> open, EventCallback opened)
    {
        if (_busy)
        {
            return; // a second tap before the button re-rendered as disabled
        }

        _busy = true;
        await OnProblem.InvokeAsync(null);
        try
        {
            switch (await open())
            {
                case ContactOutcome.Opened:
                    await opened.InvokeAsync();
                    break;
                case ContactOutcome.NotLinked:
                    await OnProblem.InvokeAsync(T.Get(Copy.ContactClosed));
                    break;
                default:
                    await OnProblem.InvokeAsync(T.Get(Copy.ContactFailed));
                    break;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Call or Text failed");
            await OnProblem.InvokeAsync(T.Get(Copy.ContactFailed));
        }
        finally
        {
            _busy = false;
        }
    }
}
