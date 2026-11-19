using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Search;

/// <summary>
/// "What do you need?" box. In <see cref="Live"/> mode every change is reported at once (the search sheet keeps the words;
/// nothing goes to the server), and the keyboard's search/enter key also runs <see cref="OnSubmit"/>; otherwise it
/// searches when they press that key. The placeholder names an
/// honorific ("Find a Manong…"): the one that fits <see cref="CategoryId"/>, or a random one until a trade is picked.
/// </summary>
public partial class SearchBox : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    /// <summary>The words to start with, e.g. the last search.</summary>
    [Parameter] public string? Value { get; set; }

    [Parameter] public bool Live { get; set; }

    /// <summary>Inside a card (the search sheet): a plain outlined pill, without the raised look of Home's box.</summary>
    [Parameter] public bool Flat { get; set; }

    private string BoxClass => Flat ? "app-pill-input" : "app-pill-input app-search-pill";

    /// <summary>The trade being searched, if any: picks the placeholder's honorific.</summary>
    [Parameter] public string? CategoryId { get; set; }

    [Parameter] public EventCallback<string> OnSearch { get; set; }

    /// <summary>The keyboard's search/enter key in <see cref="Live"/> mode (the search sheet runs the search).</summary>
    [Parameter] public EventCallback OnSubmit { get; set; }

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

    private Task OnTypedAsync(string? text)
    {
        _text = text;
        return Live ? OnSearch.InvokeAsync(text ?? string.Empty) : Task.CompletedTask;
    }

    private async Task OnKeyDownAsync(KeyboardEventArgs e)
    {
        if (e.Key != "Enter")
        {
            return;
        }

        await OnSearch.InvokeAsync(_text ?? string.Empty);
        if (Live)
        {
            await OnSubmit.InvokeAsync();
        }
    }

    private Task ClearAsync(MouseEventArgs _)
    {
        _text = string.Empty;
        return Live ? OnSearch.InvokeAsync(string.Empty) : Task.CompletedTask;
    }
}
