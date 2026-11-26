# SideGigPh.Mcp: ask an AI about usage and errors

A local **MCP server** (Model Context Protocol, the official `ModelContextProtocol` NuGet package, stdio). An AI
client such as Claude Code or VS Code starts it on your PC and can then answer questions like "what did people search
for and not find this week?" or "which crash hits the most phones since 0.2.3?" from the live database.

- **Read only.** Each tool is a fixed EF Core query on `SideGigPhDb` (`SideGigPh.AzureSQL`) with no tracking and no
  `SaveChanges`. The client only picks the window, the filters and the row count (at most 200), never the SQL.
- **Counts, not people.** No answer has a user id, install id or session id, only how many distinct people or
  phones (`McpAnswerTests` checks this). Search words and error text are already scrubbed when stored.
- **Never deployed.** It runs on the team's PC and signs in to Azure SQL with your own Microsoft (Entra) account: the
  first question opens the sign-in page in your browser, and the login is remembered after that.

## Tools

| Tool | What it answers |
|---|---|
| `events_per_week` / `events_per_day` | Each usage event per week (Monday, Philippine time) or day, plus distinct people |
| `fulfilment` | Posts created, posts matched (offer picked), jobs completed, rehires, average minutes to a pick |
| `unmet_demand` | Searches that found no worker, by town, trade, job and typed words |
| `top_searches` | What households search for: trade, job and typed words, with empty results and distinct people |
| `demand_by_trade_and_town` | Searches and empty searches per trade and town |
| `worker_funnel` | Workers signed up → profile published → first completed job |
| `sign_ups` | New accounts per day, by side (household or worker) |
| `onboarding_funnel` | Where sign-up stops: people reaching each interview question, for one side |
| `household_funnel` | Households who signed up → searched → opened the post form → posted → picked a worker → job done |
| `recent_posts` | The jobs households posted: trade, job, town, title and words (scrubbed), budget, offers, outcome (`JobPosts`) |
| `trust_reports` | Reports and blocks by reason |
| `error_summary` | Problems grouped by source and message, on the most phones first |
| `recent_errors` | The latest reports (filter by channel, kind, words) |
| `error_report` | One report in full: stack trace, trail, build, phone |
| `errors_by_version` | Crashes, errors and slow screens per build |

The usage tools answer the same questions as `analytics/weekly.sql`, with the window as a parameter. Steps, the post
form and the job of a search are counted from the first app build with them (`035-schema-app-event-steps.sql`); older rows have none.

## Set it up (once)

1. **Your database access.** In the Azure portal Query editor, as the Entra admin, give your account read access to
   these tables only:

   ```sql
   CREATE USER [you@example.com] FROM EXTERNAL PROVIDER;   -- skip if you already have a user
   GRANT SELECT ON dbo.app_events TO [you@example.com];
   GRANT SELECT ON dbo.ErrorReports TO [you@example.com];
   GRANT SELECT ON dbo.JobPosts TO [you@example.com];    -- recent_posts
   GRANT SELECT ON dbo.JobOffers TO [you@example.com];   -- recent_posts
   ```

   Add your IP on the SQL server's **Networking** page (the same rule the Query editor uses).

2. **Publish it to a folder outside the repo**, so the client runs that copy and never the repo's build output:

   ```bash
   dotnet publish SideGigPh.Mcp -c Release -o ~/.sidegigph-mcp
   ```

   The publish itself still builds in the repo's `obj/` and `bin/Release`, so run it with Visual Studio closed (or
   from a throwaway copy of the repo). Publish it again after pulling changes to the tools.

3. **Add it to Claude Code** (your user settings, not the repo):

   ```bash
   claude mcp add tarbaho-data --scope user \
     -e SQL_CONNECTION="Server=tcp:sidegigph.database.windows.net,1433;Database=SideGigPh;Encrypt=True;" \
     -- dotnet ~/.sidegigph-mcp/SideGigPh.Mcp.dll
   ```

   Other clients use the same command and `SQL_CONNECTION`. Without `SQL_CONNECTION` the server stops at once with exit code 2.

## Signing in

- **The first question** opens the Microsoft sign-in page in your browser: pick the account you granted in step 1.
  It goes to the database's own directory (the server names it, so you never type it), so an account that isn't in,
  or invited to, that directory is refused there.
  You have 5 minutes to finish (password, MFA); Claude waits.
- **After that it is silent.** The login is kept for your Windows user (Azure.Identity's encrypted token cache, and
  which account in `%LOCALAPPDATA%\SideGigPh.Mcp\sign-in.json`) until Microsoft asks you to sign in again.
- **To force a directory** (the server couldn't be reached to ask it): add `-e AZURE_TENANT_ID=<directory id>` to
  the `claude mcp add` line.
- **Sign out / switch account:** delete `%LOCALAPPDATA%\SideGigPh.Mcp\sign-in.json`; the next question asks again.
- A connection string with its own sign-in (`Authentication=Active Directory Default` after `az login`, a SQL user,
  `Integrated Security` for LocalDB) is used as it is, with no browser.

**A tool says only "An error occurred"?** The AI sees no details; the cause (a missing `GRANT`, the firewall, a
cancelled sign-in) is in the server's log on stderr. Claude Code shows it with `claude --debug`.

## Add a tool

Add a method to `Tools/AnalyticsTools.cs` or `Tools/ErrorReportTools.cs` with `[McpServerTool(Name = "…", ReadOnly = true)]`
and a `[Description]` on the method and each parameter (the AI reads these to pick the tool). Return a record declared
in the same file, keep inputs in range with `ToolInput`, and never return who. Add a test case to
`SideGigPh.Tests/Integration/McpToolTests.cs`. A tool on another table also needs that table's `GRANT SELECT` above.
