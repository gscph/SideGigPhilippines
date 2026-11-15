namespace MobileAppTheme.Services;

/// <summary>
/// Usage analytics: which screens people open and which steps they finish, counted across everyone, never tied to a
/// name or number. A provider (Google Analytics for Firebase…) is an adapter in its own project that replaces
/// <see cref="NoAnalytics"/>; until an app adds one, nothing is recorded and nothing leaves the phone.
/// Names are short snake_case words (<c>listing_published</c>); values are ids and counts the app already has
/// (a trade id, a role), never something a person typed.
/// </summary>
public interface IAnalytics
{
    /// <summary>True when a provider is recording; false for <see cref="NoAnalytics"/> or a provider that couldn't start.</summary>
    bool IsOn { get; }

    /// <summary>The person opened a screen (<paramref name="name"/>: its route with ids removed).</summary>
    void Screen(string name);

    /// <summary>Something worth counting happened, with a few details (ids and numbers as text).</summary>
    void Event(string name, IReadOnlyDictionary<string, string>? details = null);

    /// <summary>A trait of this install that reports and experiments can be split by (role, language); null clears it.</summary>
    void SetProperty(string name, string? value);

    /// <summary>The person's choice: false stops recording on this phone.</summary>
    void SetEnabled(bool enabled);
}

/// <summary>The default: records nothing, until the app adds a provider (e.g. <c>AddFirebaseAnalytics</c>).</summary>
public sealed class NoAnalytics : IAnalytics
{
    public bool IsOn => false;

    public void Screen(string name)
    {
    }

    public void Event(string name, IReadOnlyDictionary<string, string>? details = null)
    {
    }

    public void SetProperty(string name, string? value)
    {
    }

    public void SetEnabled(bool enabled)
    {
    }
}
