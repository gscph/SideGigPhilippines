using Microsoft.Data.SqlClient;
using SideGigPh.Migrate;
using SideGigPh.Tests.Integration.Support;

namespace SideGigPh.Tests.Integration;

/// <summary>
/// The deploy's migration step (SideGigPh.Migrate) on a real SQL Server: an up-to-date database gets nothing (the config
/// only when asked), and a script the database never recorded runs, is guarded enough to run over what's there, and
/// records itself.
/// </summary>
[Trait("Category", "Integration")]
public sealed class MigratorTests : IDisposable
{
    private TestDatabase? _created;
    private readonly StringWriter _output = new();
    private readonly StringWriter _errors = new();
    private static string SqlFolder => Path.Combine(AppContext.BaseDirectory, "Sql");

    // Made on first use, so a skipped test (no SQL Server) never tries.
    private TestDatabase Database => _created ??= new TestDatabase();

    public void Dispose() => _created?.Dispose();

    [IntegrationFact]
    public async Task UpToDateDatabase_GetsNothing_AndTheConfigOnlyWhenAsked()
    {
        (await Migrate()).Should().Be(0, _errors.ToString());
        _output.ToString().Should().Contain("Schema: up to date.").And.NotContain("004-config");

        (await new Migrator(Database.ConnectionString, SqlFolder, _output, _errors).RunAsync(dryRun: false, withConfig: true))
            .Should().Be(0, _errors.ToString());
        _output.ToString().Should().Contain("004-config done.");
    }

    [IntegrationFact]
    public async Task AScriptNeverRecorded_RunsAndRecordsItself()
    {
        Execute("DELETE FROM dbo.SchemaChanges WHERE Script = '011-schema-safety'");

        (await Migrate()).Should().Be(0, _errors.ToString());

        _output.ToString().Should().Contain("Schema scripts to run: 011-schema-safety");
        Scalar("SELECT COUNT(*) FROM dbo.SchemaChanges WHERE Script = '011-schema-safety'").Should().Be(1);
    }

    [IntegrationFact]
    public async Task DryRun_ChangesNothing()
    {
        Execute("DELETE FROM dbo.SchemaChanges WHERE Script = '012-schema-feedback'");

        var migrator = new Migrator(Database.ConnectionString, SqlFolder, _output, _errors);
        (await migrator.RunAsync(dryRun: true)).Should().Be(0);

        migrator.Plan.Should().Equal("012-schema-feedback");

        Scalar("SELECT COUNT(*) FROM dbo.SchemaChanges WHERE Script = '012-schema-feedback'").Should().Be(0);
    }

    private Task<int> Migrate() => new Migrator(Database.ConnectionString, SqlFolder, _output, _errors).RunAsync(dryRun: false);

    private void Execute(string sql)
    {
        using var connection = new SqlConnection(Database.ConnectionString);
        connection.Open();
        using var command = new SqlCommand(sql, connection);
        command.ExecuteNonQuery();
    }

    private int Scalar(string sql)
    {
        using var connection = new SqlConnection(Database.ConnectionString);
        connection.Open();
        using var command = new SqlCommand(sql, connection);
        return (int)command.ExecuteScalar()!;
    }
}
