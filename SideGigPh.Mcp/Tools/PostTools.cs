using System.ComponentModel;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;
using SideGigPh.AzureSQL;
using SideGigPh.Contracts;

namespace SideGigPh.Mcp.Tools;

/// <summary>
/// What households post (<c>dbo.JobPosts</c>, <c>dbo.JobOffers</c>): the job, town, budget and words of each post and
/// what came of it. Never who posted, the exact place or the photos; the words have numbers, emails and links taken out.
/// </summary>
[McpServerToolType]
public sealed class PostTools(SideGigPhDb db)
{
    /// <summary>How much of a post's description an answer carries.</summary>
    public const int MaxDescription = 200;

    [McpServerTool(Name = "recent_posts", ReadOnly = true, Idempotent = true)]
    [Description("The jobs households posted in the last N days, newest first: when (Philippine time), trade, job, town, " +
                 "title and description (numbers, emails and links taken out), budget range in pesos, the day needed, status " +
                 "(open, closed, hidden), how many offers workers sent, and whether one was picked and the job finished.")]
    public async Task<IReadOnlyList<PostSummary>> RecentPosts(
        [Description("How many days back, 1-365.")] int days = 7,
        [Description("How many rows, 1-200.")] int top = 50,
        CancellationToken cancellationToken = default)
    {
        var from = DateTimeOffset.UtcNow.AddDays(-ToolInput.Days(days));
        var rows = await db.JobPosts
            .Where(p => p.CreatedAt >= from)
            .OrderByDescending(p => p.CreatedAt)
            .Take(ToolInput.Top(top))
            .Select(p => new
            {
                p.CreatedAt, p.CategoryId, p.ServiceId, p.CityId, p.Title, p.Description, p.BudgetMinPesos, p.BudgetPesos, p.NeededOn,
                p.Status,
                Offers = db.JobOffers.Count(o => o.PostId == p.Id),
                Picked = db.JobOffers.Any(o => o.PostId == p.Id && o.Status == JobOfferStatuses.Picked),
                Done = db.JobOffers.Any(o => o.PostId == p.Id && o.Status == JobOfferStatuses.Picked
                                             && o.HouseholdDoneAt != null && o.WorkerDoneAt != null),
            })
            .ToListAsync(cancellationToken);

        return [.. rows.Select(p => new PostSummary(
            p.CreatedAt.ToOffset(TimeSpan.FromHours(8)).DateTime, p.CategoryId, p.ServiceId, p.CityId,
            ErrorReportRules.Clean(p.Title, 100), ErrorReportRules.Clean(p.Description, MaxDescription),
            p.BudgetMinPesos, p.BudgetPesos, p.NeededOn, p.Status, p.Offers, p.Picked, p.Done))];
    }
}

/// <summary>One household's post as an answer: what and where, never who.</summary>
public sealed record PostSummary(
    DateTime PostedAtPh,
    string Trade,
    string Job,
    string Town,
    string? Title,
    string? Description,
    int? BudgetFromPesos,
    int BudgetUpToPesos,
    DateOnly? NeededOn,
    string Status,
    int Offers,
    bool Picked,
    bool Done);
