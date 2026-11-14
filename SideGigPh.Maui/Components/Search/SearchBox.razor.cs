using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Search;

/// <summary>
/// "What do you need?" box. In <see cref="Live"/> mode it searches as the user types (after a
/// short pause); otherwise it searches when they press the keyboard's search/enter key. The placeholder names an
/// honorific ("Find a Manong…"): the one that fits <see cref="CategoryId"/>, or a random one until a trade is picked.
/// </summary>
public partial class SearchBox : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    /// <summary>The words to start with, e.g. the last search.</summary>
    [Parameter] public string? Value { get; set; }

    [Parameter] public bool Live { get; set; }

    /// <summary>The trade being searched, if any: picks the placeholder's honorific.</summary>
    [Parameter] public string? CategoryId { get; set; }

    [Parameter] public EventCallback<string> OnSearch { get; set; }

    private string? _text;
    private string? _lastValue;
    private readonly string _randomHonorific = Honorifics.Random();

    private string Honorific => CategoryId is null ? _randomHonorific : Honorifics.For(CategoryId);

    protected override void OnParametersSet()
    {
        if (Value != _lastValue)
        {
            _text = _lastValue = Value;
        }
    }

    private Task OnTypedAsync(string text) => Live ? OnSearch.InvokeAsync(text) : Task.CompletedTask;

    private Task OnKeyDownAsync(KeyboardEventArgs e) =>
        e.Key == "Enter" ? OnSearch.InvokeAsync(_text ?? string.Empty) : Task.CompletedTask;

    private Task ClearAsync(MouseEventArgs _)
    {
        _text = string.Empty;
        return Live ? OnSearch.InvokeAsync(string.Empty) : Task.CompletedTask;
    }
}
