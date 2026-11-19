using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Search;

/// <summary>
/// "What do you need?" box in the search sheet. Every change is reported at once (<see cref="OnTyped"/>: the sheet keeps
/// the words; nothing goes to the server), and the keyboard's search/enter key runs <see cref="OnSubmit"/>. The
/// placeholder names an
/// honorific ("Find a Manong…"): the one that fits <see cref="CategoryId"/>, or a random one until a trade is picked.
/// </summary>
public partial class SearchBox : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    /// <summary>The words to start with, e.g. the last search.</summary>
    [Parameter] public string? Value { get; set; }

    /// <summary>Inside a card (the search sheet): a plain outlined pill, without the raised look of Home's box.</summary>
    [Parameter] public bool Flat { get; set; }

    private string BoxClass => Flat ? "app-pill-input" : "app-pill-input app-search-pill";

    /// <summary>The trade being searched, if any: picks the placeholder's honorific.</summary>
    [Parameter] public string? CategoryId { get; set; }

    /// <summary>Every change of the words, cleared included.</summary>
    [Parameter] public EventCallback<string> OnTyped { get; set; }

    /// <summary>The keyboard's search/enter key (the search sheet runs the search).</summary>
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
        return OnTyped.InvokeAsync(text ?? string.Empty);
    }

    private async Task OnKeyDownAsync(KeyboardEventArgs e)
    {
        if (e.Key != "Enter")
        {
            return;
        }

        await OnTyped.InvokeAsync(_text ?? string.Empty);
        await OnSubmit.InvokeAsync();
    }

    private Task ClearAsync(MouseEventArgs _)
    {
        _text = string.Empty;
        return OnTyped.InvokeAsync(string.Empty);
    }
}
