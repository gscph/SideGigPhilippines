using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Components.Professional;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Onboarding;

/// <summary>
/// The Skilled Worker's setup right after the interview, one full-screen step each: their own photo and 2–6 work photos
/// (<see cref="ProfessionalDraft.HasProof"/>), then Publish (which asks a Google account to add
/// a verified number first). Each step's Next waits until it is done. The photo step has one thing to do, so it asks
/// for the camera and opens the front camera by itself the first time, and moves on as soon as the selfie is saved
/// (a photo from the gallery works too). One URL per step; the rules (which step, where
/// Next and Back go, where launch resumes) are in <see cref="OnboardingSteps"/>. A listing already live goes home.
/// </summary>
public partial class ProSetup : ComponentBase
{
    [Inject] private ProWorkspace Workspace { get; set; } = default!;
    [Inject] private LocalAccount Account { get; set; } = default!;
    [Inject] private BusyIndicator Busy { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;
    [Inject] private IPhotoPicker Picker { get; set; } = default!;

    [Parameter] public int? Step { get; set; }

    private bool _loaded;
    private ProSetupStep _step = ProSetupStep.Photo;
    private PhotoAddResult? _photoResult;
    private bool _busy;

    // The camera opens by itself once per visit to the photo step, not again after they back out of it.
    private bool _cameraOffered;

    private ProfessionalDraft Draft => Workspace.Draft;

    // Carries on from the interview's bar (OnboardingSteps.ProgressOf).
    private double Progress => OnboardingSteps.ProgressOf(_step);

    protected override async Task OnInitializedAsync()
    {
        if (!await Busy.RunAsync(Workspace.EnsureDraftLoadedAsync))
        {
            Nav.NavigateTo("/", replace: true);
            return;
        }

        if (Workspace.IsPublished)
        {
            Nav.NavigateTo(ProRoutes.Home, replace: true);
            return;
        }

        _loaded = true;
        ShowStep();
    }

    protected override void OnParametersSet()
    {
        if (_loaded)
        {
            ShowStep();
        }
    }

    private void ShowStep()
    {
        _step = Draft.SetupStepToShow(Step);
        _photoResult = null;
        if (_step != ProSetupStep.Photo)
        {
            _cameraOffered = false;
        }
    }

    /// <summary>How long the photo step shows before the camera opens by itself, so its reason is read before the prompt.</summary>
    private static readonly TimeSpan ReadFirst = TimeSpan.FromMilliseconds(1200);

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_loaded && _step == ProSetupStep.Photo && !Draft.HasProfilePhoto && !_cameraOffered)
        {
            _cameraOffered = true;
            await Task.Delay(ReadFirst);
            if (_step == ProSetupStep.Photo && !Draft.HasProfilePhoto)
            {
                await TakeSelfieAsync();
                StateHasChanged();
            }
        }
    }

    // Next only for a photo they already had; a new one moves on by itself.
    private EventCallback PhotoNext => Draft.HasProfilePhoto ? EventCallback.Factory.Create(this, Next) : default;

    // The camera and its permission prompt show over the step (its reason stays visible); the splash covers the upload.
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
            Next();
        }
    }

    private void Next() => Nav.NavigateTo(OnboardingSteps.NextPath(_step));

    private void GoBack() => Nav.NavigateTo(OnboardingSteps.BackPath(_step));
}
