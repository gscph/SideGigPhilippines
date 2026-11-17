using Microsoft.AspNetCore.Components;
using MobileAppTheme.Localization;
using MobileAppTheme.Services;

namespace MobileAppTheme.Components.Shared;

/// <summary>
/// An empty list, Airbnb style: an outlined icon in a soft brand-tinted circle above a short gray line
/// ("No messages yet…"), centered with room around it. A failed read comes back as an empty list, so while the phone
/// is offline (<see cref="INetworkStatus"/>) it says "No internet" instead of "nothing here", which would read as no
/// workers, no messages or a lost post. With <see cref="OnRetry"/> it also shows Try again, and reloads by itself as
/// soon as the connection is back.
/// </summary>
public partial class EmptyState : ComponentBase, IDisposable
{
    [Inject] private AppText T { get; set; } = default!;
    [Inject] private INetworkStatus Network { get; set; } = default!;

    /// <summary>The picture: an outlined MudBlazor icon that says what the list holds.</summary>
    [Parameter, EditorRequired] public string Icon { get; set; } = string.Empty;

    [Parameter, EditorRequired] public string Text { get; set; } = string.Empty;

    /// <summary>Reads the list again: Try again while offline, and on its own when the connection comes back.</summary>
    [Parameter] public EventCallback OnRetry { get; set; }

    /// <summary>Only a list read from the server (one with <see cref="OnRetry"/>) can be empty because the phone is offline.</summary>
    private bool Offline => OnRetry.HasDelegate && !Network.IsOnline;

    private bool _wasOffline;
    private bool _retrying;

    protected override void OnInitialized()
    {
        _wasOffline = Offline;
        Network.Changed += OnNetworkChanged;
    }

    private void OnNetworkChanged() => _ = InvokeAsync(async () =>
    {
        var backOnline = _wasOffline && !Offline;
        _wasOffline = Offline;
        StateHasChanged();
        if (backOnline)
        {
            await RetryAsync();
        }
    });

    private async Task RetryAsync()
    {
        if (_retrying || !OnRetry.HasDelegate)
        {
            return;
        }

        _retrying = true;
        try
        {
            await OnRetry.InvokeAsync();
        }
        finally
        {
            _retrying = false;
        }
    }

    public void Dispose() => Network.Changed -= OnNetworkChanged;
}
