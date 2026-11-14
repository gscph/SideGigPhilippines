using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Professional;

/// <summary>
/// Add and remove the pro's references (1 to 3 people they worked for, name and mobile number), on the References
/// page and in the setup after the interview. Private (never shown to customers); at least one is needed to publish.
/// A published listing keeps at least one.
/// </summary>
public partial class ReferencesEditor : ComponentBase
{
    [Inject] private ProWorkspace Workspace { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;
    [Inject] private BusyIndicator Busy { get; set; } = default!;
    [Inject] private ILogger<ReferencesEditor> Logger { get; set; } = default!;

    /// <summary>A reference was added or removed, so the page can update what depends on the count.</summary>
    [Parameter] public EventCallback OnChanged { get; set; }

    private bool _busy;
    private LocalizedText? _problem;
    private int _formVersion;

    private IReadOnlyList<WorkReference> References => Workspace.Draft.References;

    private bool IsFull => References.Count >= ProfessionalDraft.MaxReferences;

    private string? Problem => _problem is null ? null : T.Get(_problem);

    /// <summary>Adds the reference; on success the form starts empty again (new <see cref="_formVersion"/>).</summary>
    private async Task AddAsync(ReferenceInput input)
    {
        _busy = true;
        _problem = null;
        try
        {
            var result = await Busy.RunAsync(() => Workspace.AddReferenceAsync(input.Name, input.Number));
            _problem = result switch
            {
                ReferenceAddResult.Added => null,
                ReferenceAddResult.NoName => Copy.ReferenceNoName,
                ReferenceAddResult.InvalidNumber => Copy.ReferenceBadNumber,
                ReferenceAddResult.OwnNumber => Copy.ReferenceOwnNumber,
                ReferenceAddResult.Duplicate => Copy.ReferenceDuplicate,
                _ => null,
            };
            if (result == ReferenceAddResult.Added)
            {
                _formVersion++;
            }
        }
        catch (Exception ex)
        {
            // Saved on the phone; republishing the live listing failed.
            Logger.LogError(ex, "Saving a reference failed.");
            _problem = Copy.ReferenceFailed;
        }
        finally
        {
            _busy = false;
        }

        await OnChanged.InvokeAsync();
    }

    private async Task RemoveAsync(WorkReference reference)
    {
        try
        {
            await Busy.RunAsync(() => Workspace.RemoveReferenceAsync(reference));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Removing a reference failed.");
            _problem = Copy.ReferenceFailed;
        }

        await OnChanged.InvokeAsync();
    }
}
