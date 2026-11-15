using System.Text.RegularExpressions;

namespace SideGigPh.Migrate;

/// <summary>
/// Which scripts a database still needs, from the files in <c>SideGigPh.AzureSQL/Sql</c> and the names it has in
/// <c>dbo.SchemaChanges</c>. Rules (the db-migration skill):
/// <list type="bullet">
/// <item><c>001-schema.sql</c> never runs here: it's the first release, already on the live database.</item>
/// <item>Each later <c>NNN-schema-*.sql</c> runs once, in number order, and records itself in <c>dbo.SchemaChanges</c>.</item>
/// <item><c>004-config.sql</c> (the trades, jobs and answer lists; an upsert, safe to repeat) runs last, and only when
/// asked (<c>--config</c>): the owner decides when the live trades change.</item>
/// <item>Sample data (<c>002-seed</c>, <c>003-rollback-seed</c>, <c>006-seed-…</c>) never runs here.</item>
/// </list>
/// </summary>
public static partial class MigrationPlan
{
    public const string FirstRelease = "001-schema";
    public const string Config = "004-config";

    /// <summary>The scripts to run, in order: the schema scripts the database hasn't recorded, then (when asked) the config.</summary>
    public static IReadOnlyList<string> ScriptsToRun(IEnumerable<string> files, IEnumerable<string> applied, bool withConfig = false)
    {
        var done = new HashSet<string>(applied, StringComparer.OrdinalIgnoreCase);
        var names = files.Select(Path.GetFileNameWithoutExtension).OfType<string>().ToList();
        var schema = names
            .Where(IsSchemaScript)
            .Where(name => !done.Contains(name))
            .Order(StringComparer.Ordinal);
        return [.. schema, .. withConfig && names.Contains(Config) ? [Config] : Array.Empty<string>()];
    }

    /// <summary>A later schema change: <c>NNN-schema-*.sql</c>, not the first release.</summary>
    public static bool IsSchemaScript(string name) => SchemaName().IsMatch(name) && name != FirstRelease;

    /// <summary>
    /// The batches of a script, split on lines that are only <c>GO</c> (a sqlcmd/SSMS separator SQL Server itself
    /// doesn't understand). A script without <c>GO</c> is one batch.
    /// </summary>
    public static IReadOnlyList<string> Batches(string script) =>
        [.. GoLine().Split(script).Select(batch => batch.Trim()).Where(batch => batch.Length > 0)];

    [GeneratedRegex(@"^\d{3}-schema(-.+)?$")]
    private static partial Regex SchemaName();

    // A line that is only GO (any case), maybe with a -- comment; a GO inside a word or string isn't one.
    [GeneratedRegex(@"^[ \t]*GO[ \t]*(?:--[^\r\n]*)?\r?$", RegexOptions.Multiline | RegexOptions.IgnoreCase)]
    private static partial Regex GoLine();
}
