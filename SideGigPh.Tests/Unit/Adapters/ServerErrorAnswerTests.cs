using System.Collections.Immutable;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging.Abstractions;
using SideGigPh.Api;
using SideGigPh.Contracts;

namespace SideGigPh.Tests.Unit.Adapters;

/// <summary>
/// When a function crashes, the API's <c>ErrorDetailsMiddleware</c> must still answer JSON the app understands
/// (<c>{ "error": "exception" }</c>, or <c>bad-body</c> for an unreadable request) instead of an empty 500, so the app
/// shows "didn't send" and logs the cause.
/// </summary>
public class ServerErrorAnswerTests
{
    private static async Task<(int Status, ApiError? Error)> RunAsync(Exception thrown, bool startedResponse = false)
    {
        var http = new DefaultHttpContext();
        http.Response.Body = new MemoryStream();
        http.RequestServices = new ServiceCollection().AddLogging().BuildServiceProvider();
        if (startedResponse)
        {
            http.Features.Set<Microsoft.AspNetCore.Http.Features.IHttpResponseFeature>(new StartedResponse());
        }

        var context = new TestFunctionContext(http);
        var middleware = new ErrorDetailsMiddleware(NullLogger<ErrorDetailsMiddleware>.Instance);

        await middleware.Invoke(context, _ => throw thrown);

        http.Response.Body.Position = 0;
        var error = http.Response.Body.Length == 0 ? null : JsonSerializer.Deserialize(http.Response.Body, ApiJson.Default.ApiError);
        return (http.Response.StatusCode, error);
    }

    [Fact]
    public async Task Crash_IsAnswered500WithTheExceptionCode()
    {
        var (status, error) = await RunAsync(new InvalidOperationException("database down"));

        status.Should().Be(500);
        error!.Error.Should().Be(ApiErrors.Exception);
        error.Detail.Should().Contain("database down");
    }

    [Fact]
    public async Task CrashWrappedInAnAggregate_ShowsTheRealCause()
    {
        var (_, error) = await RunAsync(new AggregateException(new TimeoutException("slow")));

        error!.Detail.Should().StartWith("System.TimeoutException");
    }

    [Fact]
    public async Task UnreadableBody_IsAnswered400BadBody()
    {
        var badBody = (Exception)Activator.CreateInstance(
            typeof(ApiResults).Assembly.GetType("SideGigPh.Api.BadBodyException")!, "bad", new JsonException("x"))!;

        var (status, error) = await RunAsync(badBody);

        status.Should().Be(400);
        error!.Error.Should().Be(ApiErrors.BadBody);
    }

    [Fact]
    public async Task AnswerAlreadyStarted_LetsTheHostHandleIt()
    {
        var run = () => RunAsync(new InvalidOperationException("late"), startedResponse: true);

        await run.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task NoCrash_PassesThrough()
    {
        var http = new DefaultHttpContext();
        var called = false;

        await new ErrorDetailsMiddleware(NullLogger<ErrorDetailsMiddleware>.Instance)
            .Invoke(new TestFunctionContext(http), _ => { called = true; return Task.CompletedTask; });

        called.Should().BeTrue();
        http.Response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task NotAnHttpFunction_Rethrows()
    {
        var run = () => new ErrorDetailsMiddleware(NullLogger<ErrorDetailsMiddleware>.Instance)
            .Invoke(new TestFunctionContext(null), _ => throw new InvalidOperationException("timer"));

        await run.Should().ThrowAsync<InvalidOperationException>();
    }

    /// <summary>The bits of the Functions host's context the middleware reads: the HTTP context and the function's name.</summary>
    private sealed class TestFunctionContext(HttpContext? http) : FunctionContext
    {
        public override string InvocationId => "test";

        public override string FunctionId => "test";

        public override TraceContext TraceContext => null!;

        public override BindingContext BindingContext => null!;

        public override RetryContext RetryContext => null!;

        public override IServiceProvider InstanceServices { get; set; } = new ServiceCollection().BuildServiceProvider();

        public override FunctionDefinition FunctionDefinition { get; } = new TestDefinition();

        // The key the Functions ASP.NET Core integration uses for GetHttpContext().
        public override IDictionary<object, object> Items { get; set; } =
            http is null ? new Dictionary<object, object>() : new Dictionary<object, object> { ["HttpRequestContext"] = http };

        public override IInvocationFeatures Features => null!;
    }

    private sealed class TestDefinition : FunctionDefinition
    {
        public override ImmutableArray<FunctionParameter> Parameters => [];

        public override string PathToAssembly => string.Empty;

        public override string EntryPoint => string.Empty;

        public override string Id => "test";

        public override string Name => "TestFunction";

        public override IImmutableDictionary<string, BindingMetadata> InputBindings => ImmutableDictionary<string, BindingMetadata>.Empty;

        public override IImmutableDictionary<string, BindingMetadata> OutputBindings => ImmutableDictionary<string, BindingMetadata>.Empty;
    }

    private sealed class StartedResponse : Microsoft.AspNetCore.Http.Features.HttpResponseFeature
    {
        public override bool HasStarted => true;
    }
}
