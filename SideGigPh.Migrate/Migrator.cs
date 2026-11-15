using Microsoft.Data.SqlClient;

namespace SideGigPh.Migrate;

/// <summary>
/// Brings a database up to date: the scripts <see cref="MigrationPlan"/> picks, one at a time, stopping at the first
/// failure. A schema script that runs but doesn't record itself in <c>dbo.SchemaChanges</c> is a failure too (it would
/// run again on every deploy).
/// </summary>
public sealed class Migrator(string connectionString, string sqlFolder, TextWriter output, TextWriter errors)
{
    /// <summary>The scripts the last run found to do (filled by <see cref="RunAsync"/>, dry run or not).</summary>
    public IReadOnlyList<string> Plan { get; private set; } = [];

    /// <summary>
    /// 0 = up to date, 1 = a script failed or didn't record itself (or this isn't SideGigPh's database).
    /// <paramref name="withConfig"/> also runs <c>004-config</c>; <paramref name="dryRun"/> only says what would run.
    /// </summary>
    public async Task<int> RunAsync(bool dryRun, bool withConfig = false)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        output.WriteLine($"Connected to {connection.DataSource} / {connection.Database}.");

        if (!await TableExistsAsync(connection, "Users"))
        {
            errors.WriteLine("This database has no dbo.Users: it isn't SideGigPh's. Run 001-schema.sql by hand on a new database first.");
            return 1;
        }

        var plan = MigrationPlan.ScriptsToRun(Directory.GetFiles(sqlFolder, "*.sql"), await AppliedAsync(connection), withConfig);
        Plan = plan;
        var pending = plan.Where(MigrationPlan.IsSchemaScript).ToList();
        output.WriteLine(pending.Count == 0 ? "Schema: up to date." : $"Schema scripts to run: {string.Join(", ", pending)}");
        if (dryRun)
        {
            output.WriteLine(plan.Count == 0 ? "Dry run: nothing to run." : $"Dry run: would run {string.Join(", ", plan)}.");
            return 0;
        }

        foreach (var name in plan)
        {
            output.WriteLine($"Running {name}.sql …");
            try
            {
                foreach (var batch in MigrationPlan.Batches(await File.ReadAllTextAsync(Path.Combine(sqlFolder, name + ".sql"))))
                {
                    await using var command = new SqlCommand(batch, connection) { CommandTimeout = 300 };
                    await command.ExecuteNonQueryAsync();
                }
            }
            catch (SqlException ex)
            {
                errors.WriteLine($"::error::{name}.sql failed: {ex.Message}");
                return 1;
            }

            if (MigrationPlan.IsSchemaScript(name) && !(await AppliedAsync(connection)).Contains(name, StringComparer.OrdinalIgnoreCase))
            {
                errors.WriteLine($"::error::{name}.sql ran but didn't record itself in dbo.SchemaChanges (see the db-migration skill).");
                return 1;
            }

            output.WriteLine($"  {name} done.");
        }

        output.WriteLine("Database is up to date.");
        return 0;
    }

    private static async Task<bool> TableExistsAsync(SqlConnection connection, string table)
    {
        await using var command = new SqlCommand("SELECT CASE WHEN OBJECT_ID(@name, N'U') IS NULL THEN 0 ELSE 1 END", connection);
        command.Parameters.AddWithValue("@name", "dbo." + table);
        return (int)(await command.ExecuteScalarAsync())! == 1;
    }

    private static async Task<List<string>> AppliedAsync(SqlConnection connection)
    {
        var names = new List<string>();
        if (!await TableExistsAsync(connection, "SchemaChanges"))
        {
            return names;
        }

        await using var command = new SqlCommand("SELECT Script FROM dbo.SchemaChanges", connection);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            names.Add(reader.GetString(0));
        }

        return names;
    }
}
