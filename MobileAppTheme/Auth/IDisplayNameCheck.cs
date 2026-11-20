namespace MobileAppTheme.Auth;

/// <summary>
/// The app's say on a name before <c>NamePage</c> sets it on the account: an app whose server refuses rude names asks
/// it here, so a refused name is never saved and the person can rewrite it. The default, <see cref="AnyDisplayName"/>,
/// allows every name.
/// </summary>
public interface IDisplayNameCheck
{
    /// <summary>False only when the name was refused; a check that couldn't run allows it (the name page must still work offline).</summary>
    Task<bool> AllowsAsync(string displayName);
}

/// <summary>Default <see cref="IDisplayNameCheck"/>: every name is fine.</summary>
public sealed class AnyDisplayName : IDisplayNameCheck
{
    public Task<bool> AllowsAsync(string displayName) => Task.FromResult(true);
}
