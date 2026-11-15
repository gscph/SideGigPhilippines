using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using SideGigPh.Api;
using SideGigPh.Contracts;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services.Analytics;
using SideGigPh.Maui.Services.Api;
using SideGigPh.Maui.Services.Diagnostics;
using SideGigPh.Tests.TestSupport;

namespace SideGigPh.Tests.Unit.Diagnostics;

/// <summary>
/// The version a build carries (so an error report or an API error names the exact app and API builds that met), and
/// usage analytics (screens with ids taken out; nothing recorded unless a build turns a provider on).
/// </summary>
public class VersionAndAnalyticsTests
{
    private static readonly BuildInfo Build = new("0.1.0-rc.12", "48466c9", AppChannels.Beta);

    [Fact]
    public void ABuild_SaysItsVersion_ThreeWays()
    {
        Build.Label.Should().Be("v0.1.0-rc.12", "what the splash and the top bar show");
        Build.ClientHeader.Should().Be("0.1.0-rc.12 (beta; 48466c9)", "what every API call carries");
        Build.WithStoreBuild("12").Should().Be("0.1.0-rc.12 (12)", "what an error report carries");
        Build.WithStoreBuild("12").Length.Should().BeLessThanOrEqualTo(ErrorReportRules.MaxVersion);
    }

    [Fact]
    public void AnAssemblyWithoutTheStamp_IsAnUnknownDevBuild()
    {
        var build = BuildInfo.From(typeof(string).Assembly);

        build.Should().Be(new BuildInfo(BuildInfo.Unknown, BuildInfo.Unknown, AppChannels.Dev));
    }

    // The API project is stamped when it builds (Directory.Build.targets), with the version from Directory.Build.props.
    [Fact]
    public void TheApi_KnowsItsVersion()
    {
        ApiVersion.Current.Should().MatchRegex(@"^\d+\.\d+\.\d+(-[a-z]+(\.\d+)?)?\+\S+$", "e.g. 0.1.0-rc+48466c9 or 0.1.0-rc.57+48466c9");
        ApiVersion.From(typeof(string).Assembly).Should().Be("unknown+unknown");
    }

    [Fact]
    public async Task EveryApiCall_NamesTheAppBuild_AndAFailureNamesTheApiBuild()
    {
        var server = new RecordingServer();
        var api = new ApiClient(new ApiOptions("https://api.test/api/", "https://photos.test/", server, Build.ClientHeader),
            new FakeIdentity(TestData.Worker()), NullLogger<ApiClient>.Instance);

        using var response = await api.SendAsync(HttpMethod.Get, "users/me");
        var write = () => api.EnsureWrittenAsync(response, "save the thing");

        server.AppVersion.Should().Be("0.1.0-rc.12 (beta; 48466c9)");
        ApiClient.ApiVersionOf(response).Should().Be("0.1.0-rc.57+abc1234");
        (await write.Should().ThrowAsync<HttpRequestException>()).Which.Message.Should().StartWith("API 0.1.0-rc.57+abc1234: couldn't save the thing: 500");
    }

    [Fact]
    public void AnAnswerWithoutTheHeader_IsAnUnknownApiBuild()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.BadGateway);

        ApiClient.ApiVersionOf(response).Should().Be(BuildInfo.Unknown);
    }

    [Fact]
    public void TheServer_ReadsWhichAppCalled()
    {
        var http = new DefaultHttpContext();
        ErrorDetailsMiddleware.CallerOf(http).Should().Be("unknown", "an app from before versions");

        http.Request.Headers[ApiHeaders.AppVersion] = Build.ClientHeader;
        ErrorDetailsMiddleware.CallerOf(http).Should().Be(Build.ClientHeader);

        http.Request.Headers[ApiHeaders.AppVersion] = new string('x', 500);
        ErrorDetailsMiddleware.CallerOf(http).Should().HaveLength(ErrorDetailsMiddleware.MaxCaller, "a made-up header can't flood the log");
    }

    [Theory]
    [InlineData(200, false)]
    [InlineData(304, false)]
    [InlineData(401, false)]
    [InlineData(404, false)]
    [InlineData(400, true)]
    [InlineData(403, true)]
    [InlineData(409, true)]
    [InlineData(429, true)]
    [InlineData(500, true)]
    public void RefusalsAndFailures_AreLogged_EverydayAnswersAreNot(int status, bool logged) =>
        ErrorDetailsMiddleware.IsRefusal(status).Should().Be(logged);

    [Theory]
    [InlineData("/start/pro/3", "/start/pro/3")]
    [InlineData("/seeker/search", "/seeker/search")]
    [InlineData("/pro/aB3dE5fG7hJ9kL1mN3pQ5rS7tU9v", "/pro/[id]")]
    [InlineData("/inbox/aB3dE5fG7hJ9kL1mN3pQ5rS7tU9v?first=1", "/inbox/[id]")]
    [InlineData("/professional/inbox/aB3dE5fG7hJ9kL1mN3pQ5rS7tU9v_zY8xW6vU4tS2rQ0pO8nM6lK4jI2h", "/professional/inbox/[id]_[id]")]
    [InlineData("/report/seeker/listing/some-very-long-id-of-a-kind-we-do-not-know-yet", "/report/seeker/listing/[id]")]
    public void AScreenName_NeverCarriesAnId(string route, string screen) =>
        AppAnalytics.ScreenName(route).Should().Be(screen);

    [Fact]
    public void AtLaunch_AnalyticsLearnsTheRoleLanguageAndBuild_AndExperimentsRefresh()
    {
        var analytics = new FakeAnalytics();
        var experiments = new FakeExperiments();
        var app = new AppAnalytics(analytics, experiments, Build);

        app.Start(new OnboardingState { Role = UserRole.Professional, Language = AppLanguage.Taglish });
        app.Screen("/professional");
        app.Track(AppEvents.ListingPublished);

        analytics.Properties.Should().BeEquivalentTo(new Dictionary<string, string?>
        {
            [AppProperties.Role] = "professional",
            [AppProperties.Language] = "taglish",
            [AppProperties.BuildLabel] = "0.1.0-rc.12",
            [AppProperties.BuildChannel] = "beta",
        });
        analytics.Screens.Should().Equal(["/professional"]);
        analytics.Events.Should().Equal([AppEvents.ListingPublished]);
        experiments.Refreshes.Should().Be(1);
    }

    [Fact]
    public void BeforeARoleIsPicked_TheRoleIsUnset()
    {
        var analytics = new FakeAnalytics();

        new AppAnalytics(analytics, new NoExperiments(), Build).Start(new OnboardingState());

        analytics.Properties[AppProperties.Role].Should().BeNull();
        analytics.Properties[AppProperties.Language].Should().BeNull();
    }

    // The defaults every build has until a Firebase flag is turned on: nothing recorded, every answer the app's own.
    [Fact]
    public void WithNoProvider_NothingIsRecorded_AndEveryVariantIsTheFallback()
    {
        var app = new AppAnalytics(new NoAnalytics(), new NoExperiments(), Build);

        app.Start(new OnboardingState());
        app.Screen("/me");
        app.Track(AppEvents.FeedbackVoted);

        app.IsOn.Should().BeFalse();
        app.Variant("publish_button", "Publish").Should().Be("Publish");
        app.IsEnabled("new_search", fallback: true).Should().BeTrue();
        app.IsEnabled("new_search", fallback: false).Should().BeFalse();
    }

    [Fact]
    public void WithAProvider_TheServersVariantWins()
    {
        var experiments = new FakeExperiments { Values = { ["publish_button"] = "Go live", ["new_search"] = "true" } };
        var app = new AppAnalytics(new FakeAnalytics(), experiments, Build);

        app.Variant("publish_button", "Publish").Should().Be("Go live");
        app.IsEnabled("new_search", fallback: false).Should().BeTrue();
        app.Variant("unknown_key", "as written").Should().Be("as written");
    }

    /// <summary>Answers 500 with the API's version header, and keeps the app version the request carried.</summary>
    private sealed class RecordingServer : HttpMessageHandler
    {
        public string? AppVersion { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            AppVersion = request.Headers.TryGetValues(ApiHeaders.AppVersion, out var values) ? values.Single() : null;
            var response = new HttpResponseMessage(HttpStatusCode.InternalServerError) { Content = new StringContent("{\"error\":\"exception\"}") };
            response.Headers.Add(ApiHeaders.ApiVersion, "0.1.0-rc.57+abc1234");
            return Task.FromResult(response);
        }
    }
}
