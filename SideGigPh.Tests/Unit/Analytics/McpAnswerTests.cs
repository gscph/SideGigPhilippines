using ModelContextProtocol.Server;
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
}
