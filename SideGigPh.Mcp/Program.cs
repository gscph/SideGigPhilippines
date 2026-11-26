using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SideGigPh.AzureSQL;
using SideGigPh.Mcp.Tools;

// A local MCP server over stdio: an MCP client (Claude Code, VS Code…) starts it and asks the tools in Tools/.
//
//   SQL_CONNECTION="…" dotnet run --project SideGigPh.Mcp
//
// Azure SQL with your own Entra sign-in (az login first):
//   Server=tcp:<server>.database.windows.net,1433;Database=<db>;Authentication=Active Directory Default;Encrypt=True;
// stdout carries the protocol, so every log line goes to stderr.

var connectionString = Environment.GetEnvironmentVariable("SQL_CONNECTION");
if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.Error.WriteLine("Set SQL_CONNECTION to the database's connection string (see SideGigPh.Mcp/README.md).");
    return 2;
}

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);
builder.Services.AddDbContext<SideGigPhDb>(options => options
    .UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure())
    .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<AnalyticsTools>()
    .WithTools<ErrorReportTools>();

await builder.Build().RunAsync();
return 0;
