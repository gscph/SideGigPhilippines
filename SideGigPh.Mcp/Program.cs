using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SideGigPh.AzureSQL;
using SideGigPh.Mcp;
using SideGigPh.Mcp.Tools;

// A local MCP server over stdio: an MCP client (Claude Code, VS Code…) starts it and asks the tools in Tools/.
//
//   SQL_CONNECTION="…" [AZURE_TENANT_ID="…"] dotnet run --project SideGigPh.Mcp
//
// Azure SQL with no sign-in in the string: the first query opens the Microsoft sign-in page in the browser and the
// login is remembered (SqlSignIn), in the database's own directory, which the server names (AZURE_TENANT_ID overrides):
//   Server=tcp:<server>.database.windows.net,1433;Database=<db>;Encrypt=True;
// A string with its own sign-in (Authentication=…, a user, Integrated Security for LocalDB) is used as it is.
// stdout carries the protocol, so every log line goes to stderr.

var connectionString = Environment.GetEnvironmentVariable("SQL_CONNECTION");
if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.Error.WriteLine("Set SQL_CONNECTION to the database's connection string (see SideGigPh.Mcp/README.md).");
    return 2;
}

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);
var signIn = SqlSignIn.IsNeeded(connectionString) ? new SqlSignIn(connectionString, Environment.GetEnvironmentVariable("AZURE_TENANT_ID")) : null;
builder.Services.AddDbContext<SideGigPhDb>(options =>
{
    if (signIn is null)
    {
        options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure());
    }
    else
    {
        options.UseSqlServer(signIn.Connect(), contextOwnsConnection: true, sql => sql.EnableRetryOnFailure());
        options.AddInterceptors(signIn.Interceptor);
    }

    options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
});
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<AnalyticsTools>()
    .WithTools<FunnelTools>()
    .WithTools<PostTools>()
    .WithTools<ErrorReportTools>();

await builder.Build().RunAsync();
return 0;
