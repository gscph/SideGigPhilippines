using MobileAppTheme.Localization;

namespace MobileAppTheme.Services;

/// <summary>
/// The lines that take turns under the logo on the busy splash (<c>BusyOverlay</c>). The app supplies its own,
/// for example a different set for each kind of user; the skeleton default is <see cref="BusySplashLines"/>.
/// </summary>
public interface ISplashLines
{
    /// <summary>The lines to rotate through right now (at least one). Asked each time the splash shows.</summary>
    Task<IReadOnlyList<LocalizedText>> LinesAsync();
}

/// <summary>Default <see cref="ISplashLines"/>: short "please wait" words ("Thinking…", "Sending…").</summary>
public sealed class BusySplashLines : ISplashLines
{
    private static readonly IReadOnlyList<LocalizedText> Words =
    [
        CommonCopy.BusyWorking,
        CommonCopy.BusyThinking,
        CommonCopy.BusyProcessing,
        CommonCopy.BusyConnecting,
        CommonCopy.BusySending,
        CommonCopy.BusyChecking,
        CommonCopy.BusySaving,
        CommonCopy.BusyTidying,
        CommonCopy.BusyAlmost,
        CommonCopy.BusyHangTight,
    ];

    public Task<IReadOnlyList<LocalizedText>> LinesAsync() => Task.FromResult(Words);
}
