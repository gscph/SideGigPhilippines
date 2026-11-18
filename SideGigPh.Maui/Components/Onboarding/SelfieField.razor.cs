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
/// "Choose from your photos" is the fallback when the camera can't be used. The splash covers only the upload, never the camera or its permission prompt. Raises
/// <see cref="OnSaved"/> once a photo is saved, so the question moves on by itself.
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

    private async Task TakeSelfieAsync()
    {
        if (_busy)
        {
            return; // a second tap before the button re-rendered as disabled
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

        await SetPhotoAsync(() => Workspace.SetProfilePhotoAsync(pick));
    }

    private Task ChoosePhotoAsync() => SetPhotoAsync(Workspace.ChangeProfilePhotoAsync);

    private async Task SetPhotoAsync(Func<Task<PhotoAddResult>> save)
    {
        if (_busy)
        {
            return; // a second tap on "choose a photo" before it re-rendered as disabled (opened the picker twice)
        }

        _busy = true;
        try
        {
            _photoResult = await Busy.RunAsync(async () =>
            {
                var result = await save();
                if (result.Added > 0)
                {
                    await Account.SaveProfileAsync(); // the account's one profile photo, kept with the profile
                }

                return result;
            });
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
