using Microsoft.Data.SqlClient;
using SideGigPh.Tests.Integration.Support;

namespace SideGigPh.Tests.Integration;

/// <summary>
/// The live database is upgraded by running the schema scripts by hand: <c>001-schema.sql</c>, then each numbered
/// change (<c>NNN-schema-*.sql</c>, .claude/skills/db-migration). Someone will run a script twice, or run them all
/// again on a database that already has them, so every script must check before it creates or alters anything.
/// </summary>
[Collection(ApiCollection.Name)]
[Trait("Category", "Integration")]
public class SchemaScriptTests(ApiTestHost api)
{
    [IntegrationFact]
    public void EverySchemaScript_RunAgainOnAnUpToDateDatabase_ChangesNothingAndDoesNotFail()
    {
        var run = () => TestDatabase.RunSchemaScripts(api.ConnectionString);

        run.Should().NotThrow("each step is guarded (IF OBJECT_ID / COL_LENGTH … IS NULL)");
    }

    [IntegrationFact]
    public void EverySchemaChangeAfterTheFirstRelease_RecordsItselfInSchemaChanges()
    {
        var changes = TestDatabase.SchemaScripts
            .Select(Path.GetFileNameWithoutExtension)
            .Where(name => name != "001-schema")
            .ToList();

        Applied().Should().BeEquivalentTo(changes, "so anyone can see which changes the live database has had");
    }

    private List<string> Applied()
    {
        using var connection = new SqlConnection(api.ConnectionString);
        connection.Open();
        using var command = new SqlCommand("SELECT Script FROM dbo.SchemaChanges", connection);
        using var reader = command.ExecuteReader();
        var scripts = new List<string>();
        while (reader.Read())
        {
            scripts.Add(reader.GetString(0));
        }

        return scripts;
    }
}
