using System.Reflection;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SideGigPh.Api;
using SideGigPh.AzureSQL;
using SideGigPh.Contracts;

namespace SideGigPh.Tests.Integration.Support;

/// <summary>
/// SideGigPh.Api running in memory: every <c>[Function]</c> with an <c>[HttpTrigger]</c> is served at its declared
/// route and methods (read from the attributes, so a renamed route breaks the tests just like it would break the app),
/// with the real stores on a real SQL Server database (<see cref="TestDatabase"/>), the real token check (with
/// <see cref="TestTokens"/>' key) and photos in <see cref="InMemoryBlobContainer"/>. One host is shared by all
/// integration tests; each test uses its own people (uids and phone numbers), so tests don't see each other's data.
/// </summary>
public sealed class ApiTestHost : IAsyncLifetime
{
    private TestDatabase? _database;
    private IHost? _host;

    public TestTokens Tokens { get; } = new();

    public InMemoryBlobContainer Blobs { get; } = new();

    /// <summary>Every notification the API sent (Firebase Cloud Messaging stand-in).</summary>
    public RecordingPushSender Pushes { get; } = new();

    /// <summary>Every sign-in account the API deleted (Firebase Admin stand-in).</summary>
    public RecordingAccountDeleter Accounts { get; } = new();

    public TestServer Server => _host!.GetTestServer();

    public string ConnectionString => _database!.ConnectionString;

    /// <summary>Every function the API serves: "METHOD route".</summary>
    public static IReadOnlyList<string> Routes { get; } = FunctionMethods().SelectMany(f => f.Methods.Select(m => $"{m.ToUpperInvariant()} {f.Route}")).ToList();

    public async Task InitializeAsync()
    {
        if (!TestDatabase.IsAvailable)
        {
            return; // every integration test is skipped (IntegrationFact) and says why
        }

        _database = new TestDatabase();
        _host = await new HostBuilder()
            .ConfigureWebHost(web => web
                .UseTestServer()
                .ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddControllers(); // runs the IActionResult the functions return, with the web JSON defaults
                    services.AddLogging(logging => logging.SetMinimumLevel(LogLevel.Warning));

                    // The same registrations as SideGigPh.Api/Program.cs, with test stand-ins for Google, Blob Storage and push.
                    services.AddSingleton<BlobContainerClient>(Blobs);
                    services.AddSingleton(new FirebaseTokenValidator(TestTokens.ProjectId, Tokens.SigningKeys));
                    services.AddSingleton<CallerLimits>();
                    services.AddSideGigPhSql(_database.ConnectionString, new PhotoLinks($"{InMemoryBlobContainer.ContainerUri}/"));
                    services.AddSingleton<IPushSender>(Pushes);
                    services.AddSingleton<IAccountDeleter>(Accounts);
                    services.AddScoped<ChatNotifier>();
                })
                .Configure(app =>
                {
                    app.UseRouting();
                    app.Use(ErrorAnswersAsync);
                    app.UseEndpoints(endpoints =>
                    {
                        foreach (var function in FunctionMethods())
                        {
                            endpoints.MapMethods($"api/{function.Route}", function.Methods, context => InvokeAsync(function.Method, context));
                        }
                    });
                }))
            .StartAsync();
    }

    public async Task DisposeAsync()
    {
        if (_host is not null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }

        _database?.Dispose();
    }

    /// <summary>Moves a chat's start back by <paramref name="by"/>, as if the Seeker wrote first that long ago (reviews open 24 hours after it).</summary>
    public async Task StartChatEarlierAsync(string chatId, TimeSpan by)
    {
        await using var connection = new Microsoft.Data.SqlClient.SqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = new Microsoft.Data.SqlClient.SqlCommand(
            "UPDATE Chats SET CreatedAt = DATEADD(second, -@seconds, CreatedAt) WHERE Id = @id", connection);
        command.Parameters.AddWithValue("@seconds", (int)by.TotalSeconds);
        command.Parameters.AddWithValue("@id", chatId);
        (await command.ExecuteNonQueryAsync()).Should().Be(1, "the chat exists");
    }

    /// <summary>A raw HTTP client to the API, signed in with <paramref name="token"/> when given.</summary>
    public HttpClient Client(string? token = null)
    {
        var client = Server.CreateClient();
        client.BaseAddress = new Uri(Server.BaseAddress, "api/");
        if (token is not null)
        {
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }

        return client;
    }

    private static IEnumerable<(MethodInfo Method, string Route, string[] Methods)> FunctionMethods() =>
        typeof(PhotoFunctions).Assembly.GetTypes()
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            .Where(method => method.GetCustomAttribute<FunctionAttribute>() is not null)
            .Select(method => (method, trigger: method.GetParameters()
                .Select(p => p.GetCustomAttribute<HttpTriggerAttribute>())
                .OfType<HttpTriggerAttribute>()
                .Single()))
            .Select(f => (f.method, f.trigger.Route!, f.trigger.Methods!));

    /// <summary>Calls the function like the Functions host: the request, route values by name, and the cancellation token.</summary>
    private static async Task InvokeAsync(MethodInfo method, HttpContext context)
    {
        var function = ActivatorUtilities.CreateInstance(context.RequestServices, method.DeclaringType!);
        var arguments = method.GetParameters().Select(p =>
            p.ParameterType == typeof(HttpRequest) ? context.Request
            : p.ParameterType == typeof(CancellationToken) ? context.RequestAborted
            : (object?)context.GetRouteValue(p.Name!)?.ToString()).ToArray();

        // Like Program.cs's GzipMiddleware: a JSON answer is gzipped for a caller that accepts it.
        var result = (IActionResult)GzipMiddleware.Wrap(context.Request, await (Task<IActionResult>)method.Invoke(function, arguments)!)!;
        await result.ExecuteResultAsync(new ActionContext(context, context.GetRouteData(), new ActionDescriptor()));
    }

    /// <summary>
    /// The deployed API answers exceptions with <c>ErrorDetailsMiddleware</c> (a Functions-host middleware, tested on
    /// its own in <c>ErrorAnswersTests</c>); this does the same here so the app sees the same answers.
    /// </summary>
    private static async Task ErrorAnswersAsync(HttpContext context, Func<Task> next)
    {
        try
        {
            await next();
        }
        catch (Exception ex) when (!context.Response.HasStarted)
        {
            var badBody = ex.GetType().Name == "BadBodyException";
            context.Response.StatusCode = badBody ? StatusCodes.Status400BadRequest : StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new ApiError(badBody ? ApiErrors.BadBody : ApiErrors.Exception, ex.ToString()));
        }
    }
}

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiTestHost>
{
    public const string Name = "SideGigPh.Api with SQL";
}
