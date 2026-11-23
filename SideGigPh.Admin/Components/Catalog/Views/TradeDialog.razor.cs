using Microsoft.AspNetCore.Components;
using MudBlazor;
using SideGigPh.Admin.Services;
using SideGigPh.Contracts;

namespace SideGigPh.Admin.Components.Catalog.Views;

/// <summary>
/// A trade's names and hint, for a new trade (<see cref="Trade"/> null) or a rename. <see cref="Save"/> sends it; the
/// dialog closes only when it went through, else it shows why and keeps what was typed.
/// </summary>
public partial class TradeDialog
{
    public static readonly DialogOptions Options = new() { MaxWidth = MaxWidth.Small, FullWidth = true, CloseOnEscapeKey = true };

    [CascadingParameter] public required IMudDialogInstance Dialog { get; set; }

    /// <summary>The trade being renamed, or null for a new one.</summary>
    [Parameter] public AdminTradeDto? Trade { get; set; }

    [Parameter, EditorRequired] public Func<SaveTradeRequest, Task> Save { get; set; } = default!;

    private string? _english;
    private string? _taglish;
    private string? _hintEnglish;
    private string? _hintTaglish;
    private string? _error;
    private bool _busy;

    private bool CanSave => CatalogRules.IsTradeName(_english) && CatalogRules.IsTradeName(_taglish);

    private string Note => Trade is null
        ? "The trade comes last in the app's list, with an \"Others\" job. Add its jobs next. It shows the plus picture until it gets its own."
        : "Its id stays " + Trade.Id + ". Posts and chats already made keep the old name.";

    protected override void OnInitialized()
    {
        _english = Trade?.English;
        _taglish = Trade?.Taglish;
        _hintEnglish = Trade?.HintEnglish;
        _hintTaglish = Trade?.HintTaglish;
    }

    private void Cancel() => Dialog.Cancel();

    private async Task SaveAsync()
    {
        if (_busy || !CanSave) return;
        _busy = true;
        _error = null;
        try
        {
            await Save(new SaveTradeRequest(_english!.Trim(), _taglish!.Trim(), Blank(_hintEnglish), Blank(_hintTaglish)));
            Dialog.Close(DialogResult.Ok(true));
        }
        catch (AdminApiException ex)
        {
            _error = ex.Message;
        }
        finally
        {
            _busy = false;
        }
    }

    private static string? Blank(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();
}
