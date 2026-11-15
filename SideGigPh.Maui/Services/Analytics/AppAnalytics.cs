using SideGigPh.Contracts;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services.Diagnostics;

namespace SideGigPh.Maui.Services.Analytics;

/// <summary>The things Tarbaho counts (<see cref="AppAnalytics.Track"/>). Names are stable: reports are built on them.</summary>
public static class AppEvents
{
    /// <summary>A Skilled Worker's listing went live.</summary>
    public const string ListingPublished = "listing_published";

    /// <summary>Someone posted a suggestion on the feedback board.</summary>
    public const string FeedbackPosted = "feedback_posted";

    /// <summary>Someone voted for a suggestion on the feedback board.</summary>
    public const string FeedbackVoted = "feedback_voted";
}

/// <summary>What reports and A/B tests can be split by (<see cref="AppAnalytics.Start"/>). Never anything personal.</summary>
public static class AppProperties
{
    /// <summary><c>seeker</c> or <c>professional</c>; unset before the role is picked.</summary>
    public const string Role = "role";

    /// <summary><c>english</c> or <c>taglish</c>.</summary>
    public const string Language = "language";

    /// <summary>The build's version label, e.g. <c>0.1.0-rc.12</c>.</summary>
    public const string BuildLabel = "build_label";

    /// <summary><c>dev</c>, <c>beta</c> or <c>prod</c>.</summary>
    public const string BuildChannel = "build_channel";
}

/// <summary>
/// Tarbaho's usage analytics and A/B tests, over the skeleton's <see cref="IAnalytics"/> and <see cref="IExperiments"/>
/// ports. Both are off until a build turns on the Firebase flags (<c>Directory.Build.props</c>); then every call here
/// reaches Google Analytics, and until then every call does nothing. What is sent: the screen's route with ids taken
/// out, the events in <see cref="AppEvents"/>, and the traits in <see cref="AppProperties"/>. Never a name, a number,
/// a message or search words.
/// </summary>
public sealed class AppAnalytics(IAnalytics analytics, IExperiments experiments, BuildInfo build)
{
    /// <summary>The longest screen name sent.</summary>
    public const int MaxScreen = 100;

    /// <summary>True when a provider is recording (a build with the analytics flag on, and Firebase running).</summary>
    public bool IsOn => analytics.IsOn;

    /// <summary>
    /// Once per launch, when the saved answers are known: says who this install is in broad strokes (role, language,
    /// build), and gets the latest A/B variants in the background (the launch doesn't wait for them).
    /// </summary>
    public void Start(OnboardingState state)
    {
        SetAudience(state.Role, state.Language);
        analytics.SetProperty(AppProperties.BuildLabel, build.Version);
        analytics.SetProperty(AppProperties.BuildChannel, build.Channel);
        _ = experiments.RefreshAsync();
    }

    /// <summary>The role or language changed (picked, or changed on the Me page).</summary>
    public void SetAudience(UserRole? role, AppLanguage? language)
    {
        analytics.SetProperty(AppProperties.Role, role switch
        {
            UserRole.Seeker => Wire.Seeker,
            UserRole.Professional => Wire.Professional,
            _ => null,
        });
        analytics.SetProperty(AppProperties.Language, language switch
        {
            AppLanguage.Taglish => Wire.Taglish,
            AppLanguage.English => Wire.English,
            _ => null,
        });
    }

    /// <summary>The person opened a screen; <paramref name="path"/> is its route without the query.</summary>
    public void Screen(string path) => analytics.Screen(ScreenName(path));

    /// <summary>One of <see cref="AppEvents"/> happened, with ids or counts as details (never typed text).</summary>
    public void Track(string name, params (string Key, string Value)[] details) =>
        analytics.Event(name, details.Length == 0 ? null : details.ToDictionary(d => d.Key, d => d.Value));

    /// <summary>The A/B variant for <paramref name="key"/>; <paramref name="fallback"/> when experiments are off or the server has none.</summary>
    public string Variant(string key, string fallback) => experiments.Variant(key, fallback);

    /// <summary>A remote switch for <paramref name="key"/>; <paramref name="fallback"/> when experiments are off or the server has none.</summary>
    public bool IsEnabled(string key, bool fallback) => experiments.IsEnabled(key, fallback);

    /// <summary>
    /// A route as a screen name that can't point at a person: account ids, chat ids and photo hashes become
    /// <c>[id]</c> (the error reports' scrubber), and any other long segment (an id of a kind not known yet) too.
    /// <c>/pro/AbC…28</c> becomes <c>/pro/[id]</c>; <c>/start/pro/3</c> stays.
    /// </summary>
    public static string ScreenName(string path)
    {
        var scrubbed = ErrorReportRules.Scrub(path.Split('?', '#')[0]) ?? string.Empty;
        var segments = scrubbed.Split('/').Select(segment => segment.Length > MaxSegment ? "[id]" : segment);
        var name = string.Join('/', segments);
        return name.Length > MaxScreen ? name[..MaxScreen] : name;
    }

    /// <summary>No route word of ours is longer; anything longer is an id.</summary>
    private const int MaxSegment = 24;
}
