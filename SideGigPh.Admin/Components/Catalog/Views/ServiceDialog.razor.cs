using Microsoft.AspNetCore.Components;
using MudBlazor;
using SideGigPh.Admin.Services;
using SideGigPh.Contracts;

namespace SideGigPh.Admin.Components.Catalog.Views;

/// <summary>
/// A job's names and price guide, for a new job in <see cref="CategoryId"/> (<see cref="Service"/> null) or a rename.
/// <see cref="Save"/> sends it; the dialog closes only when it went through, else it shows why and keeps what was typed.
/// </summary>
public partial class ServiceDialog
{
    public static readonly DialogOptions Options = new() { MaxWidth = MaxWidth.Small, FullWidth = true, CloseOnEscapeKey = true };

    [CascadingParameter] public required IMudDialogInstance Dialog { get; set; }

    [Parameter, EditorRequired] public string CategoryId { get; set; } = default!;

    /// <summary>The job being renamed, or null for a new one.</summary>
    [Parameter] public AdminServiceDto? Service { get; set; }

    [Parameter, EditorRequired] public Func<SaveServiceRequest, Task> Save { get; set; } = default!;

    private string? _english;
    private string? _taglish;
    private int? _min;
    private int? _average;
    private int? _high;
    private string? _error;
    private bool _busy;

    /// <summary>All three prices or none, in order (<see cref="CatalogRules.IsPrice"/>).</summary>
    private PriceGuideDto? Price => _min is { } min && _average is { } average && _high is { } high ? new PriceGuideDto(min, average, high) : null;

    private bool PriceOk => (_min, _average, _high) is (null, null, null) || (Price is { } price && CatalogRules.IsPrice(price));

    private bool CanSave => CatalogRules.IsServiceName(_english) && CatalogRules.IsServiceName(_taglish) && PriceOk;

    private string Note => Service is null
        ? "The job goes just before \"Others\" in the trade's list. It shows the plus picture until it gets its own."
        : "Its id stays " + Service.Id + ". Posts and chats already made keep the old name.";

    protected override void OnInitialized()
    {
        _english = Service?.English;
        _taglish = Service?.Taglish;
        _min = Service?.Price?.Min;
        _average = Service?.Price?.Average;
        _high = Service?.Price?.High;
    }

    private void Cancel() => Dialog.Cancel();

    private async Task SaveAsync()
    {
        if (_busy || !CanSave) return;
        _busy = true;
        _error = null;
        try
        {
            await Save(new SaveServiceRequest(CategoryId, _english!.Trim(), _taglish!.Trim(), Price));
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
}
