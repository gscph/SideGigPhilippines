using System.ComponentModel;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;
using SideGigPh.AzureSQL;
using SideGigPh.Contracts;

namespace SideGigPh.Mcp.Tools;

/// <summary>
/// Where people stop (<c>dbo.app_events</c>): sign-ups by side, each sign-up step reached, and how far the households
/// who signed up got. Distinct people only, never who.
/// </summary>
[McpServerToolType]
public sealed class FunnelTools(SideGigPhDb db)
{
    private const int PhilippineHours = 8;

    [McpServerTool(Name = "sign_ups", ReadOnly = true, Idempotent = true)]
    [Description("New accounts per day (Philippine time) by side: 'customer' (a household) or 'worker' (a skilled worker), " +
                 "counted when the side is first picked. Last N days, today included.")]
    public async Task<IReadOnlyList<SideCount>> SignUps(
        [Description("How many days back, 1-365.")] int days = 14,
        CancellationToken cancellationToken = default)
    {
        var from = DateTime.UtcNow.AddHours(PhilippineHours).Date.AddDays(1 - ToolInput.Days(days)).AddHours(-PhilippineHours);
        var rows = await db.AppEvents
            .Where(e => e.EventName == AppEventNames.SignUp && e.ServerTs >= from)
            .GroupBy(e => new { Day = e.ServerTs.AddHours(PhilippineHours).Date, e.UserType })
            .Select(g => new SideCount(DateOnly.FromDateTime(g.Key.Day), g.Key.UserType, g.Count()))
            .ToListAsync(cancellationToken);
        return [.. rows.OrderByDescending(r => r.Day).ThenBy(r => r.Side)];
    }

    [McpServerTool(Name = "onboarding_funnel", ReadOnly = true, Idempotent = true)]
    [Description("Where sign-up stops: for one side, how many distinct people reached each sign-up step in the last N days, " +
                 "in the order the app shows them. 'role' is the 'household or worker?' fork (both sides). Worker steps: pro_skills " +
                 "(trade), pro_services (jobs), pro_rate (price), pro_towns, pro_experience, pro_photo (selfie), pro_number " +
                 "(verified mobile, skipped for some), pro_preview, publish_checklist (promises), publish_ready, then " +
                 "'published' (listing live). Household steps: seeker_category (trade), seeker_service (job), seeker_city, " +
                 "seeker_when, seeker_budget, seeker_findby (search or post), seeker_finding (done). Counted from the app build with these events on; " +
                 "earlier sign-ups show only in sign_ups.")]
    public async Task<IReadOnlyList<StepCount>> OnboardingFunnel(
        [Description("'worker' or 'household'.")] string side = "worker",
        [Description("How many days back, 1-365.")] int days = 30,
        CancellationToken cancellationToken = default)
    {
        var worker = ToolInput.Filter(side) is "worker" or "pro" or "professional";
        var steps = worker ? AppEventSteps.Worker : AppEventSteps.Household;
        var userType = worker ? AppEventUserTypes.Worker : AppEventUserTypes.Customer;
        var from = DateTime.UtcNow.AddDays(-ToolInput.Days(days));
        var reached = await db.AppEvents
            .Where(e => e.EventName == AppEventNames.OnboardingStep && e.Step != null && (e.ClientTs ?? e.ServerTs) >= from
                        && (e.UserType == userType || e.Step == AppEventSteps.Role))
            .GroupBy(e => e.Step!)
            .Select(g => new { Step = g.Key, People = g.Select(e => e.UserId).Distinct().Count() })
            .ToDictionaryAsync(r => r.Step, r => r.People, cancellationToken);

        var funnel = steps.Select(step => new StepCount(step, reached.GetValueOrDefault(step))).ToList();
        if (worker)
        {
            funnel.Add(new StepCount("published", await db.AppEvents
                .Where(e => e.EventName == AppEventNames.WorkerProfileComplete && e.ServerTs >= from)
                .Select(e => e.UserId).Distinct().CountAsync(id => id != null, cancellationToken)));
        }

        return funnel;
    }

    [McpServerTool(Name = "household_funnel", ReadOnly = true, Idempotent = true)]
    [Description("How far the households who signed up in the last N days got, ever since: signed up, searched, opened the " +
                 "post form, posted a job, picked a worker's offer, finished a job (both sides marked it done). Distinct people.")]
    public async Task<HouseholdFunnel> HouseholdFunnel(
        [Description("Sign-ups from how many days back, 1-365.")] int days = 30,
        CancellationToken cancellationToken = default)
    {
        var from = DateTime.UtcNow.AddDays(-ToolInput.Days(days));
        var cohort = db.AppEvents
            .Where(e => e.EventName == AppEventNames.SignUp && e.UserType == AppEventUserTypes.Customer && e.ServerTs >= from && e.UserId != null)
            .Select(e => e.UserId);
        var posts = db.AppEvents.Where(e => e.EventName == AppEventNames.RequestCreated && cohort.Contains(e.UserId));

        Task<int> Did(string name) => db.AppEvents
            .Where(e => e.EventName == name && cohort.Contains(e.UserId))
            .Select(e => e.UserId).Distinct().CountAsync(cancellationToken);

        // A pick and a finished job are counted on the household's post, whoever's action logged it.
        Task<int> PostsThat(string name) => posts
            .Where(p => db.AppEvents.Any(e => e.EventName == name && e.RequestId == p.RequestId))
            .Select(p => p.UserId).Distinct().CountAsync(cancellationToken);

        return new HouseholdFunnel(
            await cohort.Distinct().CountAsync(cancellationToken),
            await Did(AppEventNames.Search),
            await Did(AppEventNames.PostStarted),
            await Did(AppEventNames.RequestCreated),
            await PostsThat(AppEventNames.RequestMatched),
            await PostsThat(AppEventNames.JobCompleted));
    }

    [McpServerTool(Name = "top_searches", ReadOnly = true, Idempotent = true)]
    [Description("What households search for: searches by trade, job (a service id, e.g. 'aircon-cleaning'; empty for a " +
                 "whole-trade search or an older app) and typed words, most frequent first, with how many found no worker " +
                 "and how many distinct people searched.")]
    public async Task<IReadOnlyList<SearchTopic>> TopSearches(
        [Description("How many days back, 1-365.")] int days = 7,
        [Description("How many rows, 1-200.")] int top = 30,
        CancellationToken cancellationToken = default)
    {
        var from = DateTime.UtcNow.AddDays(-ToolInput.Days(days));
        return await db.AppEvents
            .Where(e => e.EventName == AppEventNames.Search && (e.ClientTs ?? e.ServerTs) >= from)
            .GroupBy(e => new { e.ServiceCategory, e.ServiceId, e.SearchTerm })
            .OrderByDescending(g => g.Count())
            .Take(ToolInput.Top(top))
            .Select(g => new SearchTopic(g.Key.ServiceCategory, g.Key.ServiceId, g.Key.SearchTerm, g.Count(),
                g.Count(e => e.ResultCount == 0), g.Select(e => e.UserId).Distinct().Count()))
            .ToListAsync(cancellationToken);
    }
}

/// <summary>Sign-ups on one day (Philippine time) for one side ('customer' or 'worker').</summary>
public sealed record SideCount(DateOnly Day, string? Side, int People);

/// <summary>Distinct people who reached one sign-up step.</summary>
public sealed record StepCount(string Step, int People);

/// <summary>Households who signed up in the window, and how many of them went on to each next thing.</summary>
public sealed record HouseholdFunnel(int SignedUp, int Searched, int OpenedPostForm, int Posted, int PickedAWorker, int JobDone);

/// <summary>Searches for one trade, job and typed words; <see cref="Empty"/> found no worker.</summary>
public sealed record SearchTopic(string? Trade, string? Job, string? SearchTerm, int Searches, int Empty, int People);
