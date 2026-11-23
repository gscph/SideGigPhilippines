using Microsoft.JSInterop;

namespace SideGigPh.FrontDoor;

/// <summary>
/// The page's browser calls (wwwroot/frontdoor.js): scrolling to a section, remembering the language and the sign-up,
/// the share sheet. Call only after the first render (there is no browser while the server prerenders); every call
/// swallows a failed interop, since none of these may break the page.
/// </summary>
public sealed class PageScript(IJSRuntime js) : IAsyncDisposable
{
    public const string LanguageKey = "tarbaho.lang";
    public const string JoinedKey = "tarbaho.early-access";

    private Task<IJSObjectReference>? _module;

    private Task<IJSObjectReference> Module => _module ??= js.InvokeAsync<IJSObjectReference>("import", "./frontdoor.js").AsTask();

    public Task<string?> ReadAsync(string key) => CallAsync<string?>("read", null, key);

    public Task WriteAsync(string key, string? value) => CallAsync("write", key, value);

    public Task<string?> BrowserLanguageAsync() => CallAsync<string?>("browserLanguage", null);

    public Task SetHtmlLangAsync(string language) => CallAsync("setHtmlLang", language);

    public Task<bool> IsOnlineAsync() => CallAsync("isOnline", true);

    public Task ScrollToAsync(string sectionId, string? focusSelector = null) => CallAsync("scrollToSection", sectionId, focusSelector);

    public Task ScrollToTopAsync() => CallAsync("scrollToTop");

    public Task WatchAsync<T>(string sectionId, DotNetObjectReference<T> listener) where T : class => CallAsync("watchSection", sectionId, listener);

    public Task UnwatchAsync(string sectionId) => CallAsync("unwatchSection", sectionId);

    /// <summary>"shared", "copied" or "none".</summary>
    public Task<string> ShareAsync(string title, string text, string url) => CallAsync("share", "none", title, text, url);

    private async Task CallAsync(string name, params object?[] args)
    {
        try { await (await Module).InvokeVoidAsync(name, args); }
        catch (Exception e) when (e is JSException or JSDisconnectedException or InvalidOperationException or TaskCanceledException) { }
    }

    private async Task<T> CallAsync<T>(string name, T fallback, params object?[] args)
    {
        try { return await (await Module).InvokeAsync<T>(name, args); }
        catch (Exception e) when (e is JSException or JSDisconnectedException or InvalidOperationException or TaskCanceledException) { return fallback; }
    }

    public async ValueTask DisposeAsync()
    {
        if (_module is null) return;
        try { await (await _module).DisposeAsync(); }
        catch (Exception e) when (e is JSException or JSDisconnectedException or InvalidOperationException or TaskCanceledException) { }
    }
}
