namespace SideGigPh.AzureSQL;

/// <summary>
/// The schema scripts this code needs on the database: every <c>Sql/NNN-schema-*.sql</c> after <c>001-schema.sql</c>,
/// each of which records itself in <c>dbo.SchemaChanges</c> by this name. Add the new script's name here with it
/// (a test checks the list matches the folder). <see cref="WarmupStore"/> reports any the live database is missing.
/// </summary>
public static class SchemaScripts
{
    public static readonly IReadOnlyList<string> Required =
    [
        "005-schema-ranking-signals",
        "007-schema-config-tables",
        "008-schema-service-prices",
        "009-schema-users-phone-index",
        "010-schema-error-reports",
        "011-schema-safety",
        "012-schema-feedback",
        "013-schema-geo",
        "014-schema-verification",
        "015-schema-job-posts",
        "016-schema-job-offers",
        "017-schema-others-and-ranges",
        "018-schema-job-done",
        "019-schema-construction-trade",
        "020-schema-construction-roofing",
    ];
}
