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

    public Task<string?> ReadAsync(string key) => ReadValueAsync<string?>("read", null, key);

    public Task WriteAsync(string key, string? value) => RunAsync("write", key, value);

    public Task<string?> BrowserLanguageAsync() => ReadValueAsync<string?>("browserLanguage", null);

    public Task SetHtmlLangAsync(string language) => RunAsync("setHtmlLang", language);

    /// <summary>The side ("worker") a visitor picked with a tap before Blazor had started, or null.</summary>
    public Task<string?> EarlyRoleAsync() => ReadValueAsync<string?>("earlyRole", null);

    public Task<bool> IsOnlineAsync() => ReadValueAsync("isOnline", true);

    public Task ScrollToAsync(string sectionId, string? focusSelector = null) => RunAsync("scrollToSection", sectionId, focusSelector);

    public Task ScrollToTopAsync() => RunAsync("scrollToTop");

    public Task WatchAsync<T>(string sectionId, DotNetObjectReference<T> listener) where T : class => RunAsync("watchSection", sectionId, listener);

    public Task UnwatchAsync(string sectionId) => RunAsync("unwatchSection", sectionId);

    /// <summary>"shared", "copied" or "none".</summary>
    public Task<string> ShareAsync(string title, string text, string url) => ReadValueAsync("share", "none", title, text, url);

    // Two names, never one overloaded: with one name, a call like ("scrollToSection", id, selector) bound to the
    // generic one and took the id as its fallback, so the browser got the wrong arguments.
    private async Task RunAsync(string name, params object?[] args)
    {
        try { await (await Module).InvokeVoidAsync(name, args); }
        catch (Exception e) when (e is JSException or JSDisconnectedException or InvalidOperationException or TaskCanceledException) { }
    }

    private async Task<T> ReadValueAsync<T>(string name, T fallback, params object?[] args)
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
