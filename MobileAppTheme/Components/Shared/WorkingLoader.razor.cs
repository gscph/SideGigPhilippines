using Microsoft.AspNetCore.Components;

namespace MobileAppTheme.Components.Shared;

/// <summary>
/// "Labor illusion" transition: ticks off each step the app is doing for the user's answers,
/// then raises <see cref="OnDone"/>. Visible work makes the result feel earned and personal,
/// where a bare spinner would waste the moment.
/// </summary>
public partial class WorkingLoader : ComponentBase, IDisposable
{
    private static readonly TimeSpan StepDelay = TimeSpan.FromMilliseconds(600);

    private readonly CancellationTokenSource _cts = new();
    private int _done;

    [Parameter, EditorRequired] public IReadOnlyList<string> Steps { get; set; } = [];

    [Parameter] public EventCallback OnDone { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        try
        {
            while (_done < Steps.Count)
            {
                await Task.Delay(StepDelay, _cts.Token);
                _done++;
                StateHasChanged();
            }

            // Let the last tick land before moving on.
            await Task.Delay(StepDelay / 2, _cts.Token);
            await OnDone.InvokeAsync();
        }
        catch (OperationCanceledException)
        {
            // The user navigated away mid-animation; nothing to finish.
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }
}
