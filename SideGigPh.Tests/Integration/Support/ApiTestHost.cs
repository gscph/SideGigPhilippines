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
using Microsoft.Data.SqlClient;
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

    /// <summary>The private "verification" container (ID photos and proofs), in memory.</summary>
    public InMemoryPrivateFiles PrivateFiles { get; } = new();

    /// <summary>Azure AI Document Intelligence stand-in: answers <see cref="FakeIdReader.Next"/> and counts its reads.</summary>
    public FakeIdReader IdReader { get; } = new();

    /// <summary>The one operator account (the Function App setting <c>AdminUids</c>).</summary>
    public const string AdminUid = "test-operator";

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
                    services.AddScoped<JobAlerts>();
                    services.AddSingleton<IPrivateFiles>(PrivateFiles);
                    services.AddSingleton<IIdDocumentReader>(IdReader);
                    services.AddSingleton(new AdminList(AdminUid));
                    services.AddSingleton(TimeProvider.System);
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
    public async Task StartChatEarlierAsync(string chatId, TimeSpan by) =>
        (await ExecuteAsync(
            "UPDATE Chats SET CreatedAt = DATEADD(second, -@seconds, CreatedAt) WHERE Id = @id",
            ("@seconds", (int)by.TotalSeconds), ("@id", chatId))).Should().Be(1, "the chat exists");

    /// <summary>
    /// Moves both "done" marks of the picked job on <paramref name="postId"/> back by <paramref name="by"/>, as if it
    /// was completed that long ago (blind reviews open <see cref="SideGigPh.Contracts.ReviewRules.BlindFor"/> after it).
    /// </summary>
    public async Task CompleteJobEarlierAsync(long postId, TimeSpan by) =>
        (await ExecuteAsync(
            """
            UPDATE JobOffers SET HouseholdDoneAt = DATEADD(second, -@seconds, HouseholdDoneAt), WorkerDoneAt = DATEADD(second, -@seconds, WorkerDoneAt)
            WHERE PostId = @postId AND Status = 'picked' AND HouseholdDoneAt IS NOT NULL AND WorkerDoneAt IS NOT NULL
            """,
            ("@seconds", (int)by.TotalSeconds), ("@postId", postId))).Should().Be(1, "the job is completed");

    /// <summary>
    /// Stores a reference for <paramref name="uid"/>'s listing straight in the database, as publishes did before the
    /// references feature was dropped (nothing writes them any more), so Delete account's purge can be checked.
    /// </summary>
    public async Task AddOldReferenceAsync(string uid, string mobileNumber) =>
        (await ExecuteAsync(
            "INSERT INTO ProfessionalReferences (ProfessionalId, Position, Name, MobileNumber) VALUES (@uid, 0, N'Aling Nena', @number)",
            ("@uid", uid), ("@number", mobileNumber))).Should().Be(1);

    /// <summary>Whether any offer on <paramref name="postId"/> still holds a household's exact place (<c>MeetLatitude</c>, <c>MeetSharedAt</c>).</summary>
    public async Task<bool> HoldsMeetLocationAsync(long postId) =>
        await CountAsync(
            "SELECT COUNT(*) FROM JobOffers WHERE PostId = @postId AND (MeetLatitude IS NOT NULL OR MeetLongitude IS NOT NULL OR MeetSharedAt IS NOT NULL)",
            ("@postId", postId)) > 0;

    /// <summary>Runs <c>004-config.sql</c> again on the test database, as a manual "Also run 004-config.sql" deploy does.</summary>
    public void RunConfigScript() => _database!.RunScript("004-config.sql");

    /// <summary>How many reference rows the database holds for <paramref name="uid"/>.</summary>
    public Task<int> ReferenceRowsAsync(string uid) =>
        CountAsync("SELECT COUNT(*) FROM ProfessionalReferences WHERE ProfessionalId = @uid", ("@uid", uid));

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

    /// <summary>Runs one statement on the test database; answers the rows it changed.</summary>
    private Task<int> ExecuteAsync(string sql, params (string Name, object Value)[] parameters) =>
        RunSqlAsync(sql, parameters, command => command.ExecuteNonQueryAsync());

    /// <summary>Runs one <c>SELECT COUNT(*)</c> on the test database.</summary>
    private Task<int> CountAsync(string sql, params (string Name, object Value)[] parameters) =>
        RunSqlAsync(sql, parameters, async command => (int)(await command.ExecuteScalarAsync())!);

    private async Task<int> RunSqlAsync(string sql, (string Name, object Value)[] parameters, Func<SqlCommand, Task<int>> run)
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value);
        }

        return await run(command);
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
