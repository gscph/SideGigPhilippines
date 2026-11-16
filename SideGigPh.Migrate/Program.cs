using SideGigPh.Migrate;

// Runs the SQL scripts a database hasn't had yet (MigrationPlan, Migrator), stopping at the first failure.
//
//   SQL_CONNECTION="…" dotnet run --project SideGigPh.Migrate -- --sql SideGigPh.AzureSQL/Sql [--dry-run] [--config]
//
//   --dry-run  only says what would run; under GitHub Actions it also writes pending=true|false and scripts=… to
//              $GITHUB_OUTPUT, so the workflow runs the migration job only when there is something to run.
//   --config   also runs 004-config.sql (the trades, jobs and answer lists). Never on its own: the owner asks for it.
//
// In CI (deploy-api.yml) it signs in as the deploy identity through the Azure CLI login azure/login made:
//   Server=tcp:<server>.database.windows.net,1433;Database=<db>;Authentication=Active Directory Default;Encrypt=True;
// Exit code 0 = done (or nothing to do), 1 = a script failed or didn't record itself, 2 = bad arguments.

var at = Array.IndexOf(args, "--sql");
var sqlFolder = at >= 0 && at + 1 < args.Length ? args[at + 1] : "SideGigPh.AzureSQL/Sql";
var connectionString = Environment.GetEnvironmentVariable("SQL_CONNECTION");
if (string.IsNullOrWhiteSpace(connectionString) || !Directory.Exists(sqlFolder))
{
    Console.Error.WriteLine("Set SQL_CONNECTION and pass --sql <folder with the NNN-*.sql scripts>.");
    return 2;
}

var dryRun = args.Contains("--dry-run");
var migrator = new Migrator(connectionString, sqlFolder, Console.Out, Console.Error);
var exit = await migrator.RunAsync(dryRun, withConfig: args.Contains("--config"));

if (dryRun && exit == 0 && Environment.GetEnvironmentVariable("GITHUB_OUTPUT") is { Length: > 0 } outputs)
{
    var pending = migrator.Plan.Count > 0 ? "true" : "false";
    await File.AppendAllTextAsync(outputs, $"pending={pending}\nscripts={string.Join(", ", migrator.Plan)}\n");
}

return exit;
