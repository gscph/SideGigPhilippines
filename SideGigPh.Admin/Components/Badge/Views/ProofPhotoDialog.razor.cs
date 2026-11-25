using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace SideGigPh.Admin.Components.Badge.Views;

/// <summary>
/// A proof photo in a window over the page, at its real size (never stretched): scroll to read the small print, or
/// switch to "Fit to window" to see all of it at once. The photo is the one already fetched (and logged) by the card,
/// so opening it again costs no new look.
/// </summary>
public partial class ProofPhotoDialog
{
    public static readonly DialogOptions Options = new()
    {
        MaxWidth = MaxWidth.ExtraLarge,
        FullWidth = true,
        CloseOnEscapeKey = true,
        CloseButton = true,
    };

    [CascadingParameter] public required IMudDialogInstance Dialog { get; set; }

    /// <summary>The photo as a data URL.</summary>
    [Parameter, EditorRequired] public string Src { get; set; } = string.Empty;

    /// <summary>What it is and whose ("Government ID · Juan dela Cruz").</summary>
    [Parameter] public string? Caption { get; set; }

    private bool _fit;

    private void Close() => Dialog.Close();
}
