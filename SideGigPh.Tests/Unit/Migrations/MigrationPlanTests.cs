using SideGigPh.Migrate;

namespace SideGigPh.Tests.Unit.Migrations;

/// <summary>
/// What the deploy runs on the live database (after the owner approves): the schema scripts it hasn't recorded, in
/// number order, and the config only when asked. Never the first release (already there) and never sample data.
/// </summary>
public class MigrationPlanTests
{
    private static readonly string[] Folder =
    [
        "Sql/001-schema.sql", "Sql/002-seed.sql", "Sql/003-rollback-seed.sql", "Sql/004-config.sql",
        "Sql/005-schema-ranking-signals.sql", "Sql/006-seed-search-ranking.sql", "Sql/012-schema-feedback.sql",
        "Sql/011-schema-safety.sql", "Sql/010-schema-error-reports.sql",
    ];

    [Fact]
    public void UnrecordedSchemaScripts_RunInNumberOrder_WithoutTheConfig() =>
        MigrationPlan.ScriptsToRun(Folder, ["005-schema-ranking-signals", "010-schema-error-reports"])
            .Should().Equal("011-schema-safety", "012-schema-feedback");

    [Fact]
    public void TheConfig_RunsLast_OnlyWhenAsked() =>
        MigrationPlan.ScriptsToRun(Folder, ["005-schema-ranking-signals", "010-schema-error-reports"], withConfig: true)
            .Should().Equal("011-schema-safety", "012-schema-feedback", "004-config");

    [Fact]
    public void UpToDate_RunsNothing() =>
        MigrationPlan.ScriptsToRun(Folder, ["005-schema-ranking-signals", "010-schema-error-reports", "011-schema-safety", "012-schema-feedback"])
            .Should().BeEmpty();

    [Fact]
    public void NeverTheFirstReleaseOrSampleData() =>
        MigrationPlan.ScriptsToRun(Folder, [], withConfig: true).Should().NotContain(["001-schema", "002-seed", "003-rollback-seed", "006-seed-search-ranking"]);

    [Fact]
    public void RecordedNames_MatchWhateverTheCase() =>
        MigrationPlan.ScriptsToRun(["011-schema-safety.sql"], ["011-SCHEMA-SAFETY"]).Should().BeEmpty();

    [Fact]
    public void GoLines_SplitBatches_AndAScriptWithoutGoIsOneBatch()
    {
        MigrationPlan.Batches("CREATE TABLE a (x int);\ngo\n  GO -- next\nSELECT 1;").Should().Equal("CREATE TABLE a (x int);", "SELECT 1;");
        MigrationPlan.Batches("IF 1 = 1\nBEGIN\n  SELECT 'GOOD';\nEND;").Should().ContainSingle();
    }
}
