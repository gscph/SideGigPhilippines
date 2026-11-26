using ModelContextProtocol.Server;
using SideGigPh.Mcp;
using SideGigPh.Mcp.Tools;

namespace SideGigPh.Tests.Unit.Analytics;

/// <summary>
/// What the local MCP server's tools hand to an AI client: counts and diagnostics, never who. A new answer type with a
/// user, install or session id fails here.
/// </summary>
public class McpAnswerTests
{
    [Fact]
    public void NoToolAnswer_CarriesAPersonsId()
    {
        var answers = typeof(AnalyticsTools).Assembly.GetTypes()
            .Where(t => t.Namespace == typeof(AnalyticsTools).Namespace && t.IsPublic && !t.IsAbstract
                        && !t.IsDefined(typeof(McpServerToolTypeAttribute), inherit: false))
            .ToList();

        answers.Should().Contain([typeof(EventCount), typeof(ErrorGroup), typeof(ErrorDetail)]);
        answers.SelectMany(t => t.GetProperties()).Select(p => p.Name).Should().NotContain(["UserId", "InstallId", "SessionId"]);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(500, ToolInput.MaxRows)]
    public void ToolInput_KeepsRowCountsInRange(int asked, int used) => ToolInput.Top(asked).Should().Be(used);

    [Theory]
    [InlineData(null, null)]
    [InlineData("  ", null)]
    [InlineData(" PROD ", "prod")]
    public void ToolInput_EmptyFilter_MeansAll(string? asked, string? used) => ToolInput.Filter(asked).Should().Be(used);

    [Theory]
    [InlineData("Server=tcp:x.database.windows.net,1433;Database=SideGigPh;Encrypt=True;", true)]
    [InlineData("Server=tcp:x.database.windows.net,1433;Database=SideGigPh;Authentication=Active Directory Default;", false)]
    [InlineData(@"Server=(localdb)\MSSQLLocalDB;Database=t;Integrated Security=true", false)]
    [InlineData("Server=x;Database=t;User ID=sa;Password=p", false)]
    [InlineData("Server=x;Database=t;Password=p", false)]
    public void BrowserSignIn_OnlyWhenTheStringHasNone(string connectionString, bool opensTheBrowser) =>
        SqlSignIn.IsNeeded(connectionString).Should().Be(opensTheBrowser);

    [Theory]
    [InlineData("https://login.windows.net/EC7FF1E6-C3A2-45F0-B2F5-BE56598BAE81", "ec7ff1e6-c3a2-45f0-b2f5-be56598bae81")]
    [InlineData("https://login.microsoftonline.com/contoso.onmicrosoft.com/", "contoso.onmicrosoft.com")]
    [InlineData("https://login.microsoftonline.com/common", null)]
    [InlineData(null, null)]
    [InlineData("not a url", null)]
    public void TheDatabasesTenant_IsReadFromItsSignInAuthority(string? authority, string? tenant) =>
        SqlSignIn.TenantOf(authority).Should().Be(tenant);
}
