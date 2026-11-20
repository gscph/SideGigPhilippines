using Microsoft.Data.SqlClient;

namespace SideGigPh.Tests.Integration.Support;

/// <summary>
/// A brand-new SQL Server database for one test run, built the way the live database is upgraded: <c>001-schema.sql</c>,
/// then every later schema change in number order (<see cref="SchemaScripts"/>), then the trades and jobs from
/// <c>004-config.sql</c>. A script that doesn't match the code fails here, not in Azure. Dropped afterwards.
/// <para>
/// Which server: the connection string in the <c>SIDEGIGPH_TEST_SQL</c> environment variable (CI sets it to a SQL
/// Server container), else SQL Server LocalDB on Windows (installed with Visual Studio). With neither, the
/// integration tests are skipped and say why.
/// </para>
/// </summary>
public sealed class TestDatabase : IDisposable
{
    public const string ServerVariable = "SIDEGIGPH_TEST_SQL";

    private const string LocalDb = @"Server=(localdb)\MSSQLLocalDB;Integrated Security=true;TrustServerCertificate=true";

    private static readonly Lazy<string?> Server = new(FindServer);

    private readonly string _name = $"SideGigPhTests_{Guid.NewGuid():N}";

    public TestDatabase()
    {
        if (Server.Value is not { } server)
        {
            throw new InvalidOperationException(WhyUnavailable);
        }

        using (var master = Open(server))
        {
            Execute(master, $"CREATE DATABASE [{_name}]");
        }

        ConnectionString = new SqlConnectionStringBuilder(server) { InitialCatalog = _name }.ConnectionString;
        using var db = Open(ConnectionString);
        RunSchemaScripts(ConnectionString);
        Execute(db, File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Sql", "004-config.sql")));
    }

    /// <summary>
    /// The schema scripts in the order a database gets them: <c>001-schema.sql</c> (the first release), then each
    /// <c>NNN-schema-*.sql</c> change. Data scripts (<c>002-seed</c>, <c>003-rollback-seed</c>, <c>004-config</c>) aren't schema.
    /// </summary>
    public static IReadOnlyList<string> SchemaScripts =>
        [.. Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "Sql"), "???-schema*.sql").Order(StringComparer.Ordinal)];

    /// <summary>Runs every schema script on a database, in order.</summary>
    public static void RunSchemaScripts(string connectionString)
    {
        using var db = Open(connectionString);
        foreach (var script in SchemaScripts)
        {
            Execute(db, File.ReadAllText(script));
        }
    }

    /// <summary>Runs one more script from <c>SideGigPh.AzureSQL/Sql/</c> on this database (e.g. a sample-data seed).</summary>
    public void RunScript(string fileName)
    {
        using var db = Open(ConnectionString);
        Execute(db, File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Sql", fileName)));
    }

    /// <summary>Whether a SQL Server was found; integration tests skip themselves when not.</summary>
    public static bool IsAvailable => Server.Value is not null;

    public static string WhyUnavailable =>
        $"No SQL Server for integration tests: set {ServerVariable} to a connection string, or install SQL Server LocalDB (Windows).";

    public string ConnectionString { get; }

    public void Dispose()
    {
        SqlConnection.ClearAllPools();
        using var master = Open(Server.Value!);
        Execute(master, $"ALTER DATABASE [{_name}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{_name}];");
    }

    private static string? FindServer()
    {
        var candidates = Environment.GetEnvironmentVariable(ServerVariable) is { Length: > 0 } configured
            ? [configured]
            : OperatingSystem.IsWindows() ? new[] { LocalDb } : [];

        foreach (var candidate in candidates)
        {
            try
            {
                using var connection = Open(candidate);
                return candidate;
            }
            catch (SqlException)
            {
                // Not reachable: try the next one, or skip the integration tests.
            }
        }

        return null;
    }

    private static SqlConnection Open(string connectionString)
    {
        var connection = new SqlConnection(new SqlConnectionStringBuilder(connectionString) { ConnectTimeout = 30 }.ConnectionString);
        connection.Open();
        return connection;
    }

    private static void Execute(SqlConnection connection, string sql)
    {
        using var command = new SqlCommand(sql, connection) { CommandTimeout = 120 };
        command.ExecuteNonQuery();
    }
}

/// <summary>A test that needs the database; skipped (with the reason) when no SQL Server is available.</summary>
public sealed class IntegrationFactAttribute : FactAttribute
{
    public IntegrationFactAttribute()
    {
        if (!TestDatabase.IsAvailable)
        {
            Skip = TestDatabase.WhyUnavailable;
        }
    }
}

/// <summary>A theory that needs the database; skipped (with the reason) when no SQL Server is available.</summary>
public sealed class IntegrationTheoryAttribute : TheoryAttribute
{
    public IntegrationTheoryAttribute()
    {
        if (!TestDatabase.IsAvailable)
        {
            Skip = TestDatabase.WhyUnavailable;
        }
    }
}
