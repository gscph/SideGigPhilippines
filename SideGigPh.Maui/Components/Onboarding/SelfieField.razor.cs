using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Components.Professional;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Onboarding;

/// <summary>
/// The worker's own photo (<see cref="ProfessionalDraft.HasProof"/>) in the interview's selfie question: one thing to do,
/// so "Take a selfie" asks for the camera and opens the front camera (on Android <c>Platforms/Android/AndroidPhotoPicker</c>)
/// only when tapped, never as the question opens, so the reason is on screen before the permission prompt.
/// A worker's own photo is always a selfie: there is no gallery choice here or anywhere else. The splash covers only the upload, never the camera or its permission prompt. Raises
/// <see cref="OnSaved"/> once a photo is saved, so the question moves on by itself. An upload that failed for want of
/// internet or server trouble (<see cref="PhotoAddResult.CanRetry"/>) keeps the picked photo and offers "Try again" to send
/// the same one, so nobody has to take the selfie again.
/// </summary>
public partial class SelfieField : ComponentBase
{
    [Inject] private ProWorkspace Workspace { get; set; } = default!;
    [Inject] private LocalAccount Account { get; set; } = default!;
    [Inject] private BusyIndicator Busy { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;
    [Inject] private IPhotoPicker Picker { get; set; } = default!;

    /// <summary>A photo was saved as theirs.</summary>
    [Parameter] public EventCallback OnSaved { get; set; }

    private PhotoAddResult? _photoResult;
    private bool _busy;

    // The photo whose upload failed in a way worth repeating, kept for "Try again"; null otherwise.
    private PhotoPick? _unsent;

    // The phone closed the app while the camera was open (short of memory): the selfie it took is saved for us, so the
    // question picks it up instead of asking for it again.
    protected override async Task OnInitializedAsync()
    {
        if (await Picker.RecoverSelfieAsync() is { Status: PhotoPickStatus.Picked } recovered)
        {
            await SaveAsync(recovered);
        }
    }

    private async Task TakeSelfieAsync()
    {
        if (_busy)
        {
            return; // a second tap before the button re-rendered as disabled (opened the camera twice)
        }

        _busy = true;
        PhotoPick pick;
        try
        {
            pick = await Picker.TakeSelfieAsync();
        }
        finally
        {
            _busy = false;
        }

        await SaveAsync(pick);
    }

    // "Try again": the same photo once more.
    private Task RetryAsync() => _unsent is { } pick ? SaveAsync(pick) : Task.CompletedTask;

    private async Task SaveAsync(PhotoPick pick)
    {
        if (_busy)
        {
            return;
        }

        _busy = true;
        try
        {
            _photoResult = await Busy.RunAsync(async () =>
            {
                var result = await Workspace.SetProfilePhotoAsync(pick);
                if (result.Added > 0)
                {
                    await Account.SaveProfileAsync(); // the account's one profile photo, kept with the profile
                }

                return result;
            });
            _unsent = _photoResult.CanRetry ? pick : null;
        }
        finally
        {
            _busy = false;
        }

        if (_photoResult.Added > 0)
        {
            await OnSaved.InvokeAsync();
        }
    }
}
