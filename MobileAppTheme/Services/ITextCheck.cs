namespace MobileAppTheme.Services;

/// <summary>
/// Checks text a person types before others see it (the name page). The app decides what counts as rude; the
/// skeleton's default, <see cref="NoTextCheck"/>, lets everything through.
/// </summary>
public interface ITextCheck
{
    /// <summary>True when <paramref name="text"/> should be taken out before saving.</summary>
    bool IsRude(string? text);
}

/// <summary>The default <see cref="ITextCheck"/>: nothing is refused.</summary>
public sealed class NoTextCheck : ITextCheck
{
    public bool IsRude(string? text) => false;
}
