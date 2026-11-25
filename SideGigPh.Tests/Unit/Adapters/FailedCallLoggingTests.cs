using System.Net;
using Microsoft.Extensions.Logging;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Api;
using SideGigPh.Tests.TestSupport;

namespace SideGigPh.Tests.Unit.Adapters;

/// <summary>
/// Every failed API call reaches the error reports, logged once by <see cref="ApiClient"/> whatever the caller does with
/// the exception: a post that got no answer was turned into "didn't post" by the page and never reported (seen on iOS).
/// The error-report send itself stays quiet, or a failed send would report itself.
/// </summary>
public class FailedCallLoggingTests
{
    [Theory]
    [InlineData("web")]
    [InlineData("http")]
    [InlineData("timeout")]
    public async Task Write_ThatGetsNoAnswer_IsLoggedOnce_WithoutTheQuery(string kind)
    {
        var logger = new ListLogger<ApiClient>();
        var api = Client(kind switch
        {
            "web" => new WebException("Socket closed"),
            "http" => new HttpRequestException("The network connection was lost."),
            _ => new TaskCanceledException("timed out"),
        }, logger);

        var send = () => api.SendAsync(HttpMethod.Post, "job-posts?draft=1");

        var thrown = (await send.Should().ThrowAsync<Exception>()).Which;
        SideGigPh.Maui.Services.Diagnostics.ErrorLog.WasReported(thrown).Should().BeTrue("a caller that logs it again must not make a second report");
        var error = logger.Errors.Should().ContainSingle().Subject;
        error.Should().Contain("POST").And.Contain("job-posts").And.NotContain("draft=1");
    }

    [Fact]
    public async Task Read_ThatGetsNoAnswer_IsLoggedOnce_NotAgainByReadAsync()
    {
        var logger = new ListLogger<ApiClient>();
        var api = Client(new WebException("Socket closed"), logger);

        await api.ReadAsync(async () => await api.GetAsync("search/professionals", SideGigPh.Contracts.ApiJson.Default.SearchPageDto), null, "pros");

        logger.Errors.Should().ContainSingle();
    }

    [Fact]
    public async Task Write_WithAnErrorAnswer_IsLoggedOnce()
    {
        var logger = new ListLogger<ApiClient>();
        var api = new ApiClient(new ApiOptions("https://api.test/api/", "https://photos.test/", new Answers(HttpStatusCode.InternalServerError)),
            new FakeIdentity(null), logger);

        using var response = await api.SendAsync(HttpMethod.Post, "job-posts");
        var check = () => api.EnsureWrittenAsync(response, "post the work needed");

        var thrown = (await check.Should().ThrowAsync<HttpRequestException>()).Which;
        SideGigPh.Maui.Services.Diagnostics.ErrorLog.WasReported(thrown).Should().BeTrue();
        logger.Errors.Should().ContainSingle().Which.Should().Contain("500");
    }

    [Fact]
    public async Task Publish_RefusedForTheSelfie_IsHandled_NotAnErrorReport()
    {
        // The app answers it (the selfie step asks for a new one): a refusal the app handles is no report for us to fix.
        var logger = new ListLogger<ApiClient>();
        var api = new ApiClient(new ApiOptions("https://api.test/api/", "https://photos.test/",
            new Answers(HttpStatusCode.BadRequest, "{\"error\":\"selfie-required\"}")), new FakeIdentity(null), logger);

        var result = await new ApiProfessionalProfileRepository(api).PublishAsync(TestData.ReadyDraft().ToListing());

        result.Should().Be(SideGigPh.Maui.Models.PublishResult.SelfieNeeded);
        logger.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ErrorReportSend_ThatGetsNoAnswer_LogsNothing()
    {
        var logger = new ListLogger<ApiClient>();
        var api = Client(new WebException("Socket closed"), logger);
        var reports = new ApiErrorReportRepository(api);

        var send = () => reports.SendAsync(Guid.NewGuid().ToString(), new("1.0 (1)", "abc1234", "beta", "ios 26.5", "Apple iPhone"),
            [new(SideGigPh.Maui.Models.ErrorKind.Error, "Test", "boom", null, null, null, 1, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow)]);

        var thrown = (await send.Should().ThrowAsync<HttpRequestException>()).Which;
        logger.Errors.Should().BeEmpty("a failed send of the reports must not record a report");
        SideGigPh.Maui.Services.Diagnostics.ErrorLog.WasReported(thrown).Should().BeFalse();
    }

    [Fact]
    public async Task Call_TheCallerCancelled_IsNotAFailure()
    {
        var logger = new ListLogger<ApiClient>();
        using var cancel = new CancellationTokenSource();
        await cancel.CancelAsync();
        var api = Client(new TaskCanceledException("cancelled"), logger);

        var send = () => api.SendAsync(HttpMethod.Get, "search/professionals", cancellationToken: cancel.Token);

        await send.Should().ThrowAsync<OperationCanceledException>();
        logger.Errors.Should().BeEmpty();
    }

    private static ApiClient Client(Exception failure, ILogger<ApiClient> logger) =>
        new(new ApiOptions("https://api.test/api/", "https://photos.test/", new Throwing(failure)), new FakeIdentity(null), logger);

    private sealed class Throwing(Exception failure) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => throw failure;
    }

    private sealed class Answers(HttpStatusCode status, string body = "{\"error\":\"exception\"}") : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body) });
    }

    private sealed class ListLogger<T> : ILogger<T>
    {
        private readonly List<string> _errors = [];

        public IReadOnlyList<string> Errors
        {
            get
            {
                lock (_errors)
                {
                    return [.. _errors];
                }
            }
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel >= LogLevel.Error)
            {
                lock (_errors)
                {
                    _errors.Add(formatter(state, exception));
                }
            }
        }
    }
}
